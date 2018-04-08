﻿namespace KalikoCMS.Services.Initialization {
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using Attributes;
    using Core;
    using Data.Entities;
    using Data.Repositories.Interfaces;
    using Logging;
    using Mappers;
    using Serialization;

    internal class PropertySynchronizer {
        private static readonly ILog Logger = LogProvider.For<ContentTypeSynchronizer>();

        #region Synchronize properties

        internal void SynchronizeProperties(ContentType pageType, Type type, ICollection<PropertyEntity> propertyEntities) {
            var propertyRepository = ServiceLocator.Current.GetInstance<IPropertyRepository>();
            var propertyMapper = new PropertyMapper();

            var propertyAttributeType = typeof(PropertyAttribute);
            var requiredAttributeType = typeof(RequiredAttribute);
            var properties = propertyEntities ?? new List<PropertyEntity>();
            var sortOrder = 0;

            foreach (var propertyInfo in type.GetProperties()) {
                var attributes = propertyInfo.GetCustomAttributes(true);

                var propertyAttribute = (PropertyAttribute)attributes.SingleOrDefault(propertyAttributeType.IsInstanceOfType);

                if (propertyAttribute == null) {
                    continue;
                }

                var propertyName = propertyInfo.Name;
                var declaringType = propertyInfo.PropertyType;
                var propertyTypeId = Guid.Empty; // TODO: PropertyType.GetPropertyTypeId(declaringType);

                if (!propertyAttribute.IsTypeValid(declaringType)) {
                    var notSupportedException = new NotSupportedException(string.Format("The property attribute of '{0}' on pagetype '{1}' ({2}) does not support the propertytype!", propertyName, pageType.Name, type.FullName));
                    Logger.Error("Problem", notSupportedException);
                    throw notSupportedException;
                }

                PropertyTypeBinder.RegisterType(declaringType);
                var required = attributes.Count(requiredAttributeType.IsInstanceOfType) > 0;

                sortOrder++;

                var property = properties.SingleOrDefault(p => p.Name == propertyName);

                if (property == null) {
                    property = new PropertyEntity {Name = propertyName};
                    properties.Add(property);
                }

                property.PropertyTypeId = propertyTypeId;
                property.ContentTypeId = pageType.ContentTypeId;
                property.SortOrder = sortOrder;
                property.Header = propertyAttribute.Header;
                property.Required = required;
                property.TabGroup = propertyAttribute.TabGroup;

                // If generic and standard attribute, store generic type as parameter. Required for list types like CollectionProperty.
                if (declaringType.IsGenericType && propertyAttribute.GetType() == typeof(PropertyAttribute)) {
                    var subType = declaringType.GetGenericArguments()[0];
                    property.Parameters = subType.FullName + ", " + subType.Assembly.GetName().Name;
                }
                else {
                    property.Parameters = propertyAttribute.Parameters;
                }

                if (property.PropertyId == 0) {
                    propertyRepository.Create(property);
                }

                var propertyDefinition = propertyMapper.Map(property);
                pageType.Properties.Add(propertyDefinition);

                Logger.Info(" + " + propertyDefinition.Name);
            }
        }

        #endregion

    }
}