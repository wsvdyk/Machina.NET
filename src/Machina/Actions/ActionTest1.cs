using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machina
{


    //  ████████╗███████╗███████╗████████╗     ██╗
    //  ╚══██╔══╝██╔════╝██╔════╝╚══██╔══╝    ███║
    //     ██║   █████╗  ███████╗   ██║       ╚██║
    //     ██║   ██╔══╝  ╚════██║   ██║        ██║
    //     ██║   ███████╗███████║   ██║        ██║
    //     ╚═╝   ╚══════╝╚══════╝   ╚═╝        ╚═╝
    //
    /// <summary>
    /// An Action to do something on the robot.
    /// </summary>
    class ActionTest1 : Action
    {
        public bool relative;

        public override ActionType Type => ActionType.Test1;

        public ActionTest1(bool relative) : base()
        {
            this.relative = relative;
        }

        public override string ToString()
        {
            return string.Format("Test1");
        }

        public override string ToInstruction()
        {
            return "Test1";
        }
    }
}
