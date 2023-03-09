using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HalApplicationBuilder.Core.Members;

namespace HalApplicationBuilder.Core.Runtime {

    internal class MemberMapperFromUiToDb : IMemberVisitor {
        internal MemberMapperFromUiToDb(object uiInstance, object dbInstance, RuntimeContext context) {
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

            var value = uiProp.GetValue(uiInstance);
            dbProp.SetValue(dbInstance, value);
        }

        void IMemberVisitor.Visit(Child member) {
            var childUiInstance = uiInstance
                .GetType()
                .GetProperty(member.InstanceModelPropName)
                .GetValue(uiInstance);
            var childDbEntity = context.DbSchema
                .GetDbEntity(member.ChildAggregate);
            var navigationProperty = dbInstance
                .GetType()
                .GetProperty(member.NavigationPropName);

            var childDbInstance = navigationProperty.GetValue(dbInstance);
            if (childDbInstance != null) {
                var mapper = new MemberMapperFromUiToDb(childUiInstance, childDbInstance, context);
                foreach (var m in childDbEntity.Source.Members) {
                    m.Accept(mapper);
                }

            } else {
                var newChildDbInstance = context.ConvertToDbInstance(childUiInstance, childDbEntity);
                navigationProperty.SetValue(dbInstance, newChildDbInstance);
            }
        }

        void IMemberVisitor.Visit(Variation member) {
            // 区分値(int)の設定
            var dbProp = dbInstance.GetType().GetProperty(member.DbPropName);
            var uiProp = uiInstance.GetType().GetProperty(member.InstanceModelTypeSwitchPropName);
            var value = uiProp.GetValue(uiInstance);
            dbProp.SetValue(dbInstance, value);

            // Variation子要素の設定
            foreach (var variation in member.Variations) {
                var childUiInstance = uiInstance
                    .GetType()
                    .GetProperty(member.InstanceModelTypeDetailPropName(variation))
                    .GetValue(uiInstance);
                var childDbEntity = context.DbSchema
                    .GetDbEntity(variation.Value);
                var navigationProperty = dbInstance
                    .GetType()
                    .GetProperty(member.NavigationPropName(childDbEntity));

                var childDbInstance = navigationProperty.GetValue(dbInstance);
                if (childDbInstance != null) {
                    var mapper = new MemberMapperFromUiToDb(childUiInstance, childDbInstance, context);
                    foreach (var m in childDbEntity.Source.Members) {
                        m.Accept(mapper);
                    }

                } else {
                    var newChildDbInstance = context.ConvertToDbInstance(childUiInstance, childDbEntity);
                    navigationProperty.SetValue(dbInstance, newChildDbInstance);
                }
            }
        }

        void IMemberVisitor.Visit(Children member) {
            var childDbEntity = context.DbSchema
                .GetDbEntity(member.ChildAggregate);
            var childDbProperty = dbInstance
                .GetType()
                .GetProperty(member.NavigationPropName);

            // Addメソッドはジェネリック型の方のICollectionにしかないのでリフレクションを使って呼び出す
            var collection = (IEnumerable)childDbProperty
                .GetValue(dbInstance);
            var add = collection
                .GetType()
                .GetMethod(nameof(ICollection<object>.Add));

            // キーを比較して重複あるものは上書き、ないものは新規追加、という動きを実現するためのdictionary
            var keymaps = new Dictionary<UIModel.InstanceKey, object>();
            foreach (var childDbInstance in collection) {
                keymaps.Add(UIModel.InstanceKey.Create(childDbInstance, childDbEntity), childDbInstance);
            }

            var chlidrenUiInstances = (IEnumerable)uiInstance
                .GetType()
                .GetProperty(member.InstanceModelPropName)
                .GetValue(uiInstance);

            foreach (var childUiInstance in chlidrenUiInstances) {
                var newChildDbInstance = context.ConvertToDbInstance(childUiInstance, childDbEntity);
                var pk = UIModel.InstanceKey.Create(newChildDbInstance, childDbEntity);

                if (keymaps.TryGetValue(pk, out var existDbEntity)) {
                    var mapper = new MemberMapperFromUiToDb(childUiInstance, existDbEntity, context);
                    foreach (var m in childDbEntity.Source.Members) {
                        m.Accept(mapper);
                    }

                } else {
                    add.Invoke(collection, new[] { newChildDbInstance });
                }
            }
        }

        void IMemberVisitor.Visit(Reference member) {
            var dbEntity = context.DbSchema.GetDbEntity(member.RefTarget);

            var uiProp = uiInstance.GetType().GetProperty(member.InstanceModelPropName);
            var referenceDto = (UIModel.ReferenceDTO)uiProp.GetValue(uiInstance);

            var parsed = UIModel.InstanceKey.TryParse(referenceDto.InstanceKey, dbEntity, out var instanceKey);

            var dbType = dbInstance.GetType();
            foreach (var column in member.RefPKs) {
                var dbProp = dbType.GetProperty(column.PropertyName);
                var value = parsed ? instanceKey.ValuesDictionary[column.FK] : null;
                dbProp.SetValue(dbInstance, value);
            }
        }
    }
}
