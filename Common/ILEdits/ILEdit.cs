using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projections.Common.ILEdits
{
    public interface ILEdit
    {
        void Init();
        void Deinit();
    }
}
