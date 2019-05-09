using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machina
{
    public class ActionMoveToRobTarget : Action
    {
        public RobTarget rt;
        public bool relative;

        public override ActionType Type => ActionType.MoveToRobTarget;

        public ActionMoveToRobTarget(Vector trans, double q1, double q2, double q3, double q4, double cf1, double cf4, double cf6, double cfX, bool relTrans) : base()
        {
            this.rt = RobTarget.Create(trans, q1, q2, q3, q4, cf1, cf4, cf6, cfX); // shallow copy
            this.relative = relTrans;
        }

        public override string ToString()
        {
            return string.Format("Move to {0} mm", rt.position);
        }

        public override string ToInstruction()
        {
            return string.Format("Move([{0},{1},{2}],[{3},{4},{5},{6}],[{7},{8},{9},{10}])",
                Math.Round(this.rt.position.X, Geometry.STRING_ROUND_DECIMALS_MM),
                Math.Round(this.rt.position.Y, Geometry.STRING_ROUND_DECIMALS_MM),
                Math.Round(this.rt.position.Z, Geometry.STRING_ROUND_DECIMALS_MM),
                rt.quaternion[0], rt.quaternion[1], rt.quaternion[2], rt.quaternion[3],
                rt.confdata[0], rt.confdata[1], rt.confdata[2], rt.confdata[3]
            );
        }
    }
}
