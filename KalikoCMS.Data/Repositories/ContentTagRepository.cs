﻿namespace KalikoCMS.Data.Repositories {
    using System;
    using Entities;
    using Interfaces;

    public class ContentTagRepository : RepositoryBase<ContentTagEntity, int>, IContentTagRepository {
        private readonly CmsContext _cmsContext;

        public ContentTagRepository(CmsContext cmsContext) : base(cmsContext) {
            _cmsContext = cmsContext;
        }

        public override ContentTagEntity GetById(int id) {
            throw new NotImplementedException();
        }
    }
}