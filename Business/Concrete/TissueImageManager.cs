﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Business.Abstract;
using Business.Consts;
using Business.Validations.FluentValidation;
using Core.Abstract;
using Core.Aspects.Autofac.Validation;
using Core.Utilities.BusinessRules;
using Core.Utilities.Filing;
using Core.Utilities.Filing.Database;
using Core.Utilities.Filing.Local;
using Core.Utilities.Guids;
using Core.Utilities.Result;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.AspNetCore.Http;

namespace Business.Concrete
{
    public class TissueImageManager : ITissueImageService
    {
        private ITissueImageDal _tissueImageDal;
        private LocalFileSystem _localFileSystem;
        private DatabaseFileSytem _databaseFileSytem;

        public TissueImageManager(ITissueImageDal tissueImageDal, LocalFileSystem localFileSystem, DatabaseFileSytem databaseFileSytem)
        {
            _tissueImageDal = tissueImageDal;
            _localFileSystem = localFileSystem;
            _databaseFileSytem = databaseFileSytem;
        }

        [ValidationAspect(typeof(TissueImageValidator))]
        public IResult<TissueImage> Add(IFile file, int tissueId)
        {
            var result = BusinessRules<TissueImage>.Checker(ImageCountChecker(tissueId));

            TissueImage tissueImage = new TissueImage()
            {
                ImagePath = ((ImageLocalFiling)_localFileSystem).Path,
                Guid = new GuidGenerator().Generator(),
                Date = DateTime.Now,
                TissueId = tissueId,
                Image = _databaseFileSytem.Filing(file)
            };

            if (result != null)
            {
                foreach (var error in result)
                {
                    return new FailResult<TissueImage>(error.Message);
                }
            }

            _localFileSystem.Filing(file, tissueImage.Guid);
            _tissueImageDal.Add(tissueImage);
            return new SuccessResult<TissueImage>(Messages.success);
        }

        public IResult<TissueImage> Delete(TissueImage tissueImage)
        {
            _tissueImageDal.Delete(tissueImage);
            return new SuccessResult<TissueImage>(Messages.success);
        }

        public IResult<List<TissueImage>> GetImagesPerTissue(int tissueId)
        {
            var result = BusinessRules<TissueImage>.Checker(IfImageExistCheck(tissueId));

            if (result != null)
            {
                foreach (var error in result)
                {
                    return new FailResult<List<TissueImage>>(error.Message); //default bir foto gelicek buraya
                }
            }

            return new SuccessResult<List<TissueImage>>(Messages.success, _tissueImageDal.GetAll(p => p.TissueId == tissueId));
        }

        public IResult<TissueImage> Update(TissueImage tissueImage)
        {
            tissueImage.Date = DateTime.Now;
            _tissueImageDal.Update(tissueImage);
            return new SuccessResult<TissueImage>(Messages.success);
        }

        private IResult<TissueImage> ImageCountChecker(int tissueid)
        {
            var result = _tissueImageDal.GetAll(p => p.TissueId == tissueid).Count;

            if (result == 5)
            {
                return new FailResult<TissueImage>(Messages.imageCountExceed);
            }
            return new SuccessResult<TissueImage>(Messages.success);
        }

        private IResult<TissueImage> IfImageExistCheck(int tissueId)
        {
            var result = _tissueImageDal.GetAll(p => p.TissueId == tissueId).Count;

            if (result == 0)
            {
                return new FailResult<TissueImage>(Messages.imageCountExceed);
            }
            return new SuccessResult<TissueImage>(Messages.success);
        }
    }
}
