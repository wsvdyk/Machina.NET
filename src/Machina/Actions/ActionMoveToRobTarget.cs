using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machina
{
    public class ActionMoveToRobTarget : Action
    {
        public Vector translation;
        public double[] q  = new double[4];
        public double[] cf = new double[4];
        //double q1, q2, q3, q4;
        //double cf1, cf2, cf3, cf4;
        public bool relative;

        public override ActionType Type => ActionType.MoveToRobTarget;

        public ActionMoveToRobTarget(Vector trans, double q1, double q2, double q3, double q4, double cf1, double cf2, double cf3, double cf4, bool relTrans) : base()
        {
            this.translation = new Vector(trans);  // shallow copy
            this.q[0] = q1; this.q[1] = q2; this.q[2] = q3; this.q[3] = q4;
            this.cf[0] = cf1; this.cf[1] = cf2; this.cf[2] = cf3; this.cf[3] = cf4;
            this.relative = relTrans;
        }

        public override string ToString()
        {
            return string.Format("Move to {0} mm", translation);
        }

        public override string ToInstruction()
        {
            return string.Format("Move([{0},{1},{2}],[{3},{4},{5},{6}],[{7},{8},{9},{10}])",
                Math.Round(this.translation.X, Geometry.STRING_ROUND_DECIMALS_MM),
                Math.Round(this.translation.Y, Geometry.STRING_ROUND_DECIMALS_MM),
                Math.Round(this.translation.Z, Geometry.STRING_ROUND_DECIMALS_MM),
                q[0], q[1], q[2], q[3],
                cf[0], cf[1], cf[2], cf[3]
            );
        }
    }
}
