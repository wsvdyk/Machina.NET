using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machina
{
    public class ActionMovecToRobTarget : Action
    {
        public RobTarget rt1;
        public RobTarget rt2;
        public bool relative;

        public override ActionType Type => ActionType.MovecToRobTarget;

        public ActionMovecToRobTarget(Vector trans1, double[,] data1, Vector trans2, double[,] data2, bool relTrans) : base()
        {
            this.rt1 = RobTarget.Create(trans1, data1[0,0], data1[0, 1], data1[0, 2], data1[0, 3], data1[1, 0], data1[1, 1], data1[1, 2], data1[1, 3]); // shallow copy
            this.rt2 = RobTarget.Create(trans2, data2[0, 0], data2[0, 1], data2[0, 2], data2[0, 3], data2[1, 0], data2[1, 1], data2[1, 2], data2[1, 3]);
            this.relative = relTrans;
        }

        public override string ToString()
        {
            return string.Format("Move circularly to {0} mm via {1}", rt2.position, rt1.position);
        }

        public override string ToInstruction()
        {
            return string.Format("Movec([{0},{1},{2}],[{3},{4},{5},{6}],[{7},{8},{9},{10}], \n [{11},{12},{13}],[{14},{15},{16},{17}],[{18},{19},{20},{21}])",
                Math.Round(this.rt1.position.X, Geometry.STRING_ROUND_DECIMALS_MM),
                Math.Round(this.rt1.position.Y, Geometry.STRING_ROUND_DECIMALS_MM),
                Math.Round(this.rt1.position.Z, Geometry.STRING_ROUND_DECIMALS_MM),
                rt1.quaternion[0], rt1.quaternion[1], rt1.quaternion[2], rt1.quaternion[3],
                rt1.confdata[0], rt1.confdata[1], rt1.confdata[2], rt1.confdata[3],
                Math.Round(this.rt2.position.X, Geometry.STRING_ROUND_DECIMALS_MM),
                Math.Round(this.rt2.position.Y, Geometry.STRING_ROUND_DECIMALS_MM),
                Math.Round(this.rt2.position.Z, Geometry.STRING_ROUND_DECIMALS_MM),
                rt2.quaternion[0], rt2.quaternion[1], rt2.quaternion[2], rt2.quaternion[3],
                rt2.confdata[0], rt2.confdata[1], rt2.confdata[2], rt2.confdata[3]
            );
        }
    }
}
