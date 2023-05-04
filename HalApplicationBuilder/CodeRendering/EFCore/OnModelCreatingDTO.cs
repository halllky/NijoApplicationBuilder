using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalApplicationBuilder.CodeRendering.EFCore {

    internal class OnModelCreatingDTO {
        internal required string OpponentName { get; init; }
        internal required E_Multiplicity Multiplicity { get; init; }
        internal required IEnumerable<RenderedProperty> ForeignKeys { get; init; }
        internal required Microsoft.EntityFrameworkCore.DeleteBehavior OnDelete { get; init; }

        [Flags]
        internal enum E_Multiplicity {
            HasOne = 1,
            HasMany = 2,
            WithOne = 4,
            WithMany = 8,

            HasOneWithOne = 5,
            HasOneWithMany = 9,
            HasManyWithOne = 6,
            HasManyWithMany = 10,
        }
    }
}
