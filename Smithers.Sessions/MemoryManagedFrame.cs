using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Smithers.Serialization;

namespace Smithers.Sessions
{
    public class MemoryManagedFrame
    {
        int _index;
        MemoryFrame _frame;
        DateTime _arrivedTime;

        public int Index 
        { 
            get { return _index; }
            set { _index = value;  }
        }

        public MemoryFrame Frame
        {
            get { return _frame; }
        }

        public MemoryManagedFrame()
        {
            _index = -1;
            _frame = new MemoryFrame();
        }

        public DateTime ArrivedTime 
        {
            get { return _arrivedTime; }
            set { _arrivedTime = value; }
        }


    }
}
