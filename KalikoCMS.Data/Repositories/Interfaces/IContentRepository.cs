﻿namespace KalikoCMS.Data.Repositories.Interfaces {
    using System;
    using System.Collections.Generic;
    using Core;
    using Entities;
    using Infrastructure;

    public interface IContentRepository : IRepository<ContentEntity, Guid> {
        IEnumerable<ContentNode> GetContentNodes();
        void SaveContent(Content content);
        void PublishContent(Content content);
    }
}