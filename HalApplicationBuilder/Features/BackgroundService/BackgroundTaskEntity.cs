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

        internal static void EditSchema(AppSchemaBuilder builder) {
            builder.AddAggregate(new[] { GraphNodeId.Value });

            builder.AddAggregateMember(new[] { GraphNodeId.Value, COL_ID }, new AggregateMemberBuildOption {
                IsPrimary = true,
                IsDisplayName = false,
                MemberType = MemberTypeResolver.TYPE_ID,
                IsRequired = true,
                InvisibleInGui = false,
            });
            builder.AddAggregateMember(new[] { GraphNodeId.Value, COL_NAME }, new AggregateMemberBuildOption {
                IsPrimary = false,
                IsDisplayName = true,
                MemberType = MemberTypeResolver.TYPE_WORD,
                IsRequired = true,
                InvisibleInGui = false,
            });
            builder.AddAggregateMember(new[] { GraphNodeId.Value, COL_BATCHTYPE }, new AggregateMemberBuildOption {
                IsPrimary = false,
                IsDisplayName = false,
                MemberType = MemberTypeResolver.TYPE_WORD,
                IsRequired = true,
                InvisibleInGui = false,
            });
            builder.AddAggregateMember(new[] { GraphNodeId.Value, COL_PARAMETERJSON }, new AggregateMemberBuildOption {
                IsPrimary = false,
                IsDisplayName = false,
                MemberType = MemberTypeResolver.TYPE_WORD,
                IsRequired = true,
                InvisibleInGui = false,
            });
            builder.AddAggregateMember(new[] { GraphNodeId.Value, COL_STATE }, new AggregateMemberBuildOption {
                IsPrimary = false,
                IsDisplayName = false,
                MemberType = ENUM_BGTASKSTATE,
                IsRequired = true,
                InvisibleInGui = false,
            });
            builder.AddAggregateMember(new[] { GraphNodeId.Value, COL_REQUESTTIME }, new AggregateMemberBuildOption {
                IsPrimary = false,
                IsDisplayName = false,
                MemberType = MemberTypeResolver.TYPE_DATETIME,
                IsRequired = true,
                InvisibleInGui = false,
            });
            builder.AddAggregateMember(new[] { GraphNodeId.Value, COL_STARTTIME }, new AggregateMemberBuildOption {
                IsPrimary = false,
                IsDisplayName = false,
                MemberType = MemberTypeResolver.TYPE_DATETIME,
                IsRequired = false,
                InvisibleInGui = false,
            });
            builder.AddAggregateMember(new[] { GraphNodeId.Value, COL_FINISHTIME }, new AggregateMemberBuildOption {
                IsPrimary = false,
                IsDisplayName = false,
                MemberType = MemberTypeResolver.TYPE_DATETIME,
                IsRequired = false,
                InvisibleInGui = false,
            });

            builder.AddEnum(ENUM_BGTASKSTATE, new[] {
                new EnumValueOption { Value = 0, Name = ENUM_BGTASKSTATE_WAITTOSTART },
                new EnumValueOption { Value = 1, Name = ENUM_BGTASKSTATE_RUNNING },
                new EnumValueOption { Value = 2, Name = ENUM_BGTASKSTATE_SUCCESS },
                new EnumValueOption { Value = 3, Name = ENUM_BGTASKSTATE_FAULT },
            });
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
