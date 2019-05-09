using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machina
{
    
    //  ██████╗  ██████╗ ██████╗ ████████╗ █████╗ ██████╗  ██████╗ ███████╗████████╗
    //  ██╔══██╗██╔═══██╗██╔══██╗╚══██╔══╝██╔══██╗██╔══██╗██╔════╝ ██╔════╝╚══██╔══╝
    //  ██████╔╝██║   ██║██████╔╝   ██║   ███████║██████╔╝██║  ███╗█████╗     ██║   
    //  ██╔══██╗██║   ██║██╔══██╗   ██║   ██╔══██║██╔══██╗██║   ██║██╔══╝     ██║   
    //  ██║  ██║╚██████╔╝██████╔╝   ██║   ██║  ██║██║  ██║╚██████╔╝███████╗   ██║   
    //  ╚═╝  ╚═╝ ╚═════╝ ╚═════╝    ╚═╝   ╚═╝  ╚═╝╚═╝  ╚═╝ ╚═════╝ ╚══════╝   ╚═╝                                                                     

    public class RobTarget : IInstructable
    {

        public Vector position { get; internal set; }

        public double[] quaternion { get; internal set; }

        public double[] confdata { get; internal set; }

        private RobTarget(Vector position, double q1, double q2, double q3, double q4, double cf1, double cf4, double cf6, double cfX)
        {
            this.position = position;
            this.quaternion = new double[] { q1, q2, q3, q4 };
            this.confdata = new double[] { cf1, cf4, cf6, cfX };
        }

        static public RobTarget Create(Vector position, double q1, double q2, double q3, double q4, double cf1, double cf4, double cf6, double cfX)
        {
            return new RobTarget(position, q1, q2, q3, q4, cf1, cf4, cf6, cfX);
        }

        public override string ToString()
        {
            return string.Format("Move to {0} mm", position);
        }

        /// <summary>
        /// Converts this Tool object to message-compatible instruction.
        /// </summary>
        /// <returns></returns>
        public string ToInstruction()
        {
            return string.Format("Move([{0},{1},{2}],[{3},{4},{5},{6}],[{7},{8},{9},{10}])",
                Math.Round(this.position.X, Geometry.STRING_ROUND_DECIMALS_MM),
                Math.Round(this.position.Y, Geometry.STRING_ROUND_DECIMALS_MM),
                Math.Round(this.position.Z, Geometry.STRING_ROUND_DECIMALS_MM),
                this.quaternion[0], this.quaternion[1], this.quaternion[2], this.quaternion[3],
                this.confdata[0], this.confdata[1], this.confdata[2], this.confdata[3]
            );
        }


    }
}
