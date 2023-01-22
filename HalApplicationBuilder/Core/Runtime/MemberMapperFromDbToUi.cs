using System;
using System.Collections;
using System.Collections.Generic;
using HalApplicationBuilder.Core.Members;

namespace HalApplicationBuilder.Core.Runtime {
    public class MemberMapperFromDbToUi : IMemberVisitor {
        internal MemberMapperFromDbToUi(object dbInstance, object uiInstance, RuntimeContext context) {
            this.uiInstance = uiInstance;
            this.dbInstance = dbInstance;
            this.context = context;
        }

        private readonly object uiInstance;
        private readonly object dbInstance;
        private readonly RuntimeContext context;

        void IMemberVisitor.Visit(SchalarValue member) {
            var dbProp = dbInstance.GetType().GetProperty(member.DbColumnPropName);
            var uiProp = uiInstance.GetType().GetProperty(member.InstanceModelPropName);

            var value = dbProp.GetValue(dbInstance);
            uiProp.SetValue(uiInstance, value);
        }

        void IMemberVisitor.Visit(Child member) {
            var dbProperty = dbInstance
                .GetType()
                .GetProperty(member.NavigationPropName);
            var childDbInstance = dbProperty
                .GetValue(dbInstance);

            if (childDbInstance != null) {
                var childDbEntity = context.DbSchema.GetDbEntity(member.ChildAggregate);
                var childUiInstance = context.ConvertToUiInstance(childDbInstance, childDbEntity);
                var childUiProperty = uiInstance
                    .GetType()
                    .GetProperty(member.InstanceModelPropName);

                childUiProperty.SetValue(uiInstance, childUiInstance);
            }
        }

        void IMemberVisitor.Visit(Variation member) {
            // 区分値(int)の設定
            var dbProp = dbInstance.GetType().GetProperty(member.DbPropName);
            var uiProp = uiInstance.GetType().GetProperty(member.InstanceModelTypeSwitchPropName);
            var value = dbProp.GetValue(dbInstance);
            uiProp.SetValue(uiInstance, value);

            // Variation子要素の設定
            foreach (var variation in member.Variations) {
                var childDbEntity = context.DbSchema
                    .GetDbEntity(variation.Value);
                var childDbProperty = dbInstance
                    .GetType()
                    .GetProperty(member.NavigationPropName(childDbEntity));
                var childDbInstance = childDbProperty
                    .GetValue(dbInstance);

                if (childDbInstance != null) {
                    var childUiProperty = uiInstance
                        .GetType()
                        .GetProperty(member.InstanceModelTypeDetailPropName(variation));
                    var childUiInstance = context.ConvertToUiInstance(childDbInstance, childDbEntity);

                    childUiProperty.SetValue(uiInstance, childUiInstance);
                }
            }
        }

        void IMemberVisitor.Visit(Children member) {
            var childDbProperty = dbInstance
                .GetType()
                .GetProperty(member.NavigationPropName);
            var childDbInstanceList = (IEnumerable)childDbProperty
                .GetValue(dbInstance);

            var childUiProperty = uiInstance
                .GetType()
                .GetProperty(member.InstanceModelPropName);
            var childUiType = childUiProperty
                .PropertyType
                .GetGenericArguments()[0];
            var childUiInstanceList = (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(childUiType));

            var childDbEntity = context.DbSchema.GetDbEntity(member.ChildAggregate);
            foreach (var childDbInstance in childDbInstanceList) {
                var childUiInstance = context.ConvertToUiInstance(childDbInstance, childDbEntity);
                childUiInstanceList.Add(childUiInstance);
            }

            childUiProperty.SetValue(uiInstance, childUiInstanceList);
        }

        void IMemberVisitor.Visit(Reference member) {
            var dbEntity = context.DbSchema.GetDbEntity(member.RefTarget);
            var instanceKey = UIModel.InstanceKey.Create(dbInstance, dbEntity);
            var uiProp = uiInstance.GetType().GetProperty(member.InstanceModelPropName);
            var referenceDto = (UIModel.ReferenceDTO)uiProp.GetValue(uiInstance);
            referenceDto.InstanceKey = instanceKey.StringValue;
            referenceDto.InstanceName = UIModel.InstanceName.Create(dbInstance, dbEntity).Value;
        }
    }
}
