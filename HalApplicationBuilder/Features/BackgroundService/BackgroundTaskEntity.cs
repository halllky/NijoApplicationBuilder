using HalApplicationBuilder.Core;
using HalApplicationBuilder.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.Features.BackgroundService {
    internal class BackgroundTaskEntity {
        internal static NodeId GraphNodeId => new NodeId($"HALAPP::{CLASSNAME}");
        internal static IEFCoreEntity CreateEntity() {
            var columns = new[] {
                new MemberOptions {
                    MemberName = COL_ID,
                    IsKey = true,
                    IsDisplayName = false,
                    MemberType = new Core.AggregateMemberTypes.Id(),
                    IsRequired = true,
                    InvisibleInGui = false,
                },
                new MemberOptions {
                    MemberName = COL_NAME,
                    IsKey = false,
                    IsDisplayName = true,
                    MemberType = new Core.AggregateMemberTypes.Word(),
                    IsRequired = true,
                    InvisibleInGui = false,
                },
                new MemberOptions {
                    MemberName = COL_BATCHTYPE,
                    IsKey = false,
                    IsDisplayName = false,
                    MemberType = new Core.AggregateMemberTypes.Word(),
                    IsRequired = true,
                    InvisibleInGui = false,
                },
                new MemberOptions {
                    MemberName = COL_PARAMETERJSON,
                    IsKey = false,
                    IsDisplayName = false,
                    MemberType = new Core.AggregateMemberTypes.Word(),
                    IsRequired = true,
                    InvisibleInGui = false,
                },
                new MemberOptions {
                    MemberName = COL_STATE,
                    IsKey = false,
                    IsDisplayName = false,
                    MemberType = new Core.AggregateMemberTypes.EnumList(CreateBackgroundTaskStateEnum()),
                    IsRequired = true,
                    InvisibleInGui = false,
                },
                new MemberOptions {
                    MemberName = COL_REQUESTTIME,
                    IsKey = false,
                    IsDisplayName = false,
                    MemberType = new Core.AggregateMemberTypes.TimePoint(),
                    IsRequired = true,
                    InvisibleInGui = false,
                },
                new MemberOptions {
                    MemberName = COL_STARTTIME,
                    IsKey = false,
                    IsDisplayName = false,
                    MemberType = new Core.AggregateMemberTypes.TimePoint(),
                    IsRequired = false,
                    InvisibleInGui = false,
                },
                new MemberOptions {
                    MemberName = COL_FINISHTIME,
                    IsKey = false,
                    IsDisplayName = false,
                    MemberType = new Core.AggregateMemberTypes.TimePoint(),
                    IsRequired = false,
                    InvisibleInGui = false,
                },
            };
            return new DbTable(GraphNodeId, CLASSNAME, columns);
        }
        internal static EnumDefinition CreateBackgroundTaskStateEnum() {
            if (!EnumDefinition.TryCreate(ENUM_BGTASKSTATE, new[] {
                new EnumDefinition.Item { Value = 0, DisplayName = "起動待ち", PhysicalName = ENUM_BGTASKSTATE_WAITTOSTART },
                new EnumDefinition.Item { Value = 1, DisplayName = "実行中", PhysicalName = ENUM_BGTASKSTATE_RUNNING },
                new EnumDefinition.Item { Value = 2, DisplayName = "正常終了", PhysicalName = ENUM_BGTASKSTATE_SUCCESS },
                new EnumDefinition.Item { Value = 3, DisplayName = "異常終了", PhysicalName = ENUM_BGTASKSTATE_FAULT },
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
    }
}
