using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machina
{
    class ActionTest2 : Action
    {
        public bool relative;

        public override ActionType Type => ActionType.Test2;

        public ActionTest2(bool relative) : base()
        {
            this.relative = relative;
        }

        public override string ToString()
        {
            return string.Format("Test2");
        }

        public override string ToInstruction()
        {
            return "Test2";
        }
    }
}
