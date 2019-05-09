using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machina
{
    //  ██████╗ ███████╗███████╗██╗███╗   ██╗███████╗████████╗ ██████╗  ██████╗ ██╗     
    //  ██╔══██╗██╔════╝██╔════╝██║████╗  ██║██╔════╝╚══██╔══╝██╔═══██╗██╔═══██╗██║     
    //  ██║  ██║█████╗  █████╗  ██║██╔██╗ ██║█████╗     ██║   ██║   ██║██║   ██║██║     
    //  ██║  ██║██╔══╝  ██╔══╝  ██║██║╚██╗██║██╔══╝     ██║   ██║   ██║██║   ██║██║     
    //  ██████╔╝███████╗██║     ██║██║ ╚████║███████╗   ██║   ╚██████╔╝╚██████╔╝███████╗
    //  ╚═════╝ ╚══════╝╚═╝     ╚═╝╚═╝  ╚═══╝╚══════╝   ╚═╝    ╚═════╝  ╚═════╝ ╚══════╝
    //                                                                                  
    /// <summary>
    /// Defines a new Tool on the Robot that will be available for Attach/Detach
    /// </summary>
    public class ActionAbbDefineTool : Action
    {
        public Tool2 tool;

        public override ActionType Type => ActionType.AbbDefineTool;

        public ActionAbbDefineTool(Tool2 tool)
        {
            this.tool = tool;
        }

        public ActionAbbDefineTool(string name,
            Point position, double [] orient,
            double weight,
            double cogX, double cogY, double cogZ) : base()
        {
            this.tool = Tool2.Create(name,
                position, orient, weight,
                cogX, cogY, cogZ);
        }

        public override string ToString()
        {
            return string.Format("Define tool \"{0}\" on the Robot.", this.tool.name);
        }

        public override string ToInstruction()
        {
            return $"DefineTool(\"{tool.name}\"," +
                $"{tool.TCPPosition.X}," +
                $"{tool.TCPPosition.Y}," +
                $"{tool.TCPPosition.Z}," +
                $"{tool.TCPOrientation[0]}," +
                $"{tool.TCPOrientation[1]}," +
                $"{tool.TCPOrientation[2]}," +
                $"{tool.TCPOrientation[3]}," +
                $"{tool.Weight}," +
                $"{tool.CenterOfGravity.X}," +
                $"{tool.CenterOfGravity.Y}," +
                $"{tool.CenterOfGravity.Z});";
        }
    }
}
