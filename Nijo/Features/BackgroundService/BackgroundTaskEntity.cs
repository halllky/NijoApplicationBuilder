using Nijo.Core;
using Nijo.Util.DotnetEx;
using Nijo.Util.CodeGenerating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Features.BackgroundService {

    partial class BackgroundTask {
        internal static NodeId GraphNodeId => new NodeId($"NIJO::{ENTITY_CLASSNAME}");

        private static void AddBgTaskEntity(AppSchemaBuilder builder) {

            builder.AddAggregate(new[] { GraphNodeId.Value }, new AggregateBuildOption {
                Handler = "view",
            });

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

        private const string ENTITY_CLASSNAME = "BackgroundTaskEntity";
        private const string LAUNCHER_CLASSNAME = "BackgroundTaskLauncher";

        private const string COL_ID = "Id";
        private const string COL_NAME = "Name";
        private const string COL_BATCHTYPE = "BatchType";
        private const string COL_PARAMETERJSON = "ParameterJson";
        private const string COL_STATE = "State";
        private const string COL_REQUESTTIME = "RequestTime";
        private const string COL_STARTTIME = "StartTime";
        private const string COL_FINISHTIME = "FinishTime";

        private const string ENUM_BGTASKSTATE = "E_BackgroundTaskState";
        private const string ENUM_BGTASKSTATE_WAITTOSTART = "WaitToStart";
        private const string ENUM_BGTASKSTATE_RUNNING = "Running";
        private const string ENUM_BGTASKSTATE_SUCCESS = "Success";
        private const string ENUM_BGTASKSTATE_FAULT = "Fault";
    }
}
