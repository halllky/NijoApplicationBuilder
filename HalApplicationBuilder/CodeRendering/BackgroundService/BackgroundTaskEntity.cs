using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.BackgroundService {
    internal class BackgroundTaskEntity {
        internal static NodeId GraphNodeId => new NodeId($"HALAPP::{CLASSNAME}");
        internal static EFCoreEntity CreateEntity() {
            var columns = new[] {
                new EFCoreEntity.SchalarMemberNotRelatedToAggregate {
                    PropertyName = COL_ID,
                    IsPrimary = true,
                    IsInstanceName = false,
                    MemberType = new Core.AggregateMembers.Id(),
                    RequiredAtDB = true,
                },
                new EFCoreEntity.SchalarMemberNotRelatedToAggregate {
                    PropertyName = COL_NAME,
                    IsPrimary = false,
                    IsInstanceName = true,
                    MemberType = new Core.AggregateMembers.Word(),
                    RequiredAtDB = true,
                },
                new EFCoreEntity.SchalarMemberNotRelatedToAggregate {
                    PropertyName = COL_BATCHTYPE,
                    IsPrimary = false,
                    IsInstanceName = false,
                    MemberType = new Core.AggregateMembers.Word(),
                    RequiredAtDB = true,
                },
                new EFCoreEntity.SchalarMemberNotRelatedToAggregate {
                    PropertyName = COL_PARAMETERJSON,
                    IsPrimary = false,
                    IsInstanceName = false,
                    MemberType = new Core.AggregateMembers.Word(),
                    RequiredAtDB = true,
                },
                new EFCoreEntity.SchalarMemberNotRelatedToAggregate {
                    PropertyName = COL_STATE,
                    IsPrimary = false,
                    IsInstanceName = false,
                    MemberType = new Core.AggregateMembers.EnumList(CreateBackgroundTaskStateEnum()),
                    RequiredAtDB = true,
                },
                new EFCoreEntity.SchalarMemberNotRelatedToAggregate {
                    PropertyName = COL_REQUESTTIME,
                    IsPrimary = false,
                    IsInstanceName = false,
                    MemberType = new Core.AggregateMembers.Word(),
                    RequiredAtDB = true,
                },
                new EFCoreEntity.SchalarMemberNotRelatedToAggregate {
                    PropertyName = COL_STARTTIME,
                    IsPrimary = false,
                    IsInstanceName = false,
                    MemberType = new Core.AggregateMembers.Word(),
                    RequiredAtDB = false,
                },
                new EFCoreEntity.SchalarMemberNotRelatedToAggregate {
                    PropertyName = COL_FINISHTIME,
                    IsPrimary = false,
                    IsInstanceName = false,
                    MemberType = new Core.AggregateMembers.Word(),
                    RequiredAtDB = false,
                },
            };
            return new EFCoreEntity(GraphNodeId, CLASSNAME, columns);
        }
        internal static EnumDefinition CreateBackgroundTaskStateEnum() {
            if (!EnumDefinition.TryCreate(ENUM_BGTASKSTATE, new[] {
                new EnumDefinition.Item { Value = 0, Name = ENUM_BGTASKSTATE_WAITTOSTART },
                new EnumDefinition.Item { Value = 1, Name = ENUM_BGTASKSTATE_RUNNING },
                new EnumDefinition.Item { Value = 2, Name = ENUM_BGTASKSTATE_SUCCESS },
                new EnumDefinition.Item { Value = 3, Name = ENUM_BGTASKSTATE_FAULT },
            }, out var enumDefinition, out var errors)) {
                throw new InvalidOperationException(errors.Join(Environment.NewLine));
            }
            return enumDefinition;
        }

        internal const string CLASSNAME = "BackgroundTaskEntity";

        internal const string COL_ID = "Id";
        internal const string COL_NAME = "Name";
        internal const string COL_BATCHTYPE = "BatchType";
        internal const string COL_PARAMETERJSON = "ParameterJson";
        internal const string COL_STATE = "State";
        internal const string COL_REQUESTTIME = "RequestTime";
        internal const string COL_STARTTIME = "StartTime";
        internal const string COL_FINISHTIME = "FinishTime";

        internal const string ENUM_BGTASKSTATE = "E_BackgroundTaskState";
        internal const string ENUM_BGTASKSTATE_WAITTOSTART = "WaitToStart";
        internal const string ENUM_BGTASKSTATE_RUNNING = "Running";
        internal const string ENUM_BGTASKSTATE_SUCCESS = "Success";
        internal const string ENUM_BGTASKSTATE_FAULT = "Fault";

        internal static Search.SearchFeature CreateSearchFeature(DirectedGraph graph, CodeRenderingContext ctx) {
            var bgTaskEntity = graph
                .Single(node => node.Item.Id == GraphNodeId)
                .As<EFCoreEntity>();
            return new Search.SearchFeature(bgTaskEntity, ctx);
        }
    }
}
