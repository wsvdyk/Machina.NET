using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Machina
{

    //  ████████╗ ██████╗  ██████╗ ██╗     
    //  ╚══██╔══╝██╔═══██╗██╔═══██╗██║     
    //     ██║   ██║   ██║██║   ██║██║     
    //     ██║   ██║   ██║██║   ██║██║     
    //     ██║   ╚██████╔╝╚██████╔╝███████╗
    //     ╚═╝    ╚═════╝  ╚═════╝ ╚══════╝
    //                                     

    /// <summary>
    /// Represents a tool object that can be attached to the end effector of the robot.
    /// This class is public and will be used directly by the user, so careful design of the API
    /// vs. internal methods will be relevant. 
    /// </summary>
    public class Tool2 : IInstructable
    {
        /// <summary>
        /// Gets a Tool object representing no tool attached. 
        /// </summary>
        //public static Tool Unset => new Tool("noTool", Point.Origin, Orientation.WorldXY, 0, Point.Origin);
        public static Tool2 Unset => new Tool2("noTool", 0, 0, 0, 1, 0, 0, 0, 0.001, 0, 0, 0.001);

        public string name { get; internal set; }

        /// <summary>
        /// Position of the Tool Center Point (TCP) relative to the Tool's base coordinate system. 
        /// In other words, if the Tool gets attached to the robot flange in XYZ [0, 0, 0], where is the tooltip relative to this?
        /// </summary>
        public Point TCPPosition { get; internal set; }

        /// <summary>
        /// Orientation of the Tool Center Point (TCP) relative to the Tool's base coordinate system. 
        /// In other words, if the Tool gets attached to the robot flange in XYZ [0, 0, 0], what is the relative rotation?
        /// </summary>
        public double[] TCPOrientation { get; internal set; }

        /// <summary>
        /// Weight of the tool in Kg.
        /// </summary>
        public double Weight { get; internal set; }

        /// <summary>
        /// Position of the Tool's CoG relative to the flange.
        /// </summary>
        public Vector CenterOfGravity { get; internal set; }

        // For the time being, tools will be defined through position (first) and orientation
        internal bool translationFirst = true;

        private Tool2(string name, Point position,
            double[] orient,double weight, double cogX, double cogY, double cogZ)
        {
            this.name = name;
            this.TCPPosition = position;
            this.TCPOrientation = orient;
            this.Weight = weight;
            this.CenterOfGravity = new Point(cogX, cogY, cogZ);
        }

        private Tool2(string name, double tcpX, double tcpY, double tcpZ,
            double tcpOrient1, double tcpOrient2, double tcpOrient3, double tcpOrient4,
            double weight, double cogX, double cogY, double cogZ)
        {
            this.name = name;
            this.TCPPosition = new Point(tcpX, tcpY, tcpZ);
            this.TCPOrientation = new double[] { tcpOrient1, tcpOrient2, tcpOrient3, tcpOrient4 };
            this.Weight = weight;
            this.CenterOfGravity = new Point(cogX, cogY, cogZ);
        }

        /// <summary>
        /// Create a new Tool object as a clone of another one. 
        /// </summary>
        /// <param name="tool"></param>
        /// <returns></returns>
        static public Tool2 Create(Tool2 tool)
        {
            return new Tool2(tool.name,
                tool.TCPPosition.X, tool.TCPPosition.Y, tool.TCPPosition.Z,
                tool.TCPOrientation[0],tool.TCPOrientation[1],
                tool.TCPOrientation[1],tool.TCPOrientation[3],
                tool.Weight,
                tool.CenterOfGravity.X, tool.CenterOfGravity.Y, tool.CenterOfGravity.Z);
        }

        /// <summary>
        /// Create a new Tool object by defining the Position and Orientation of the 
        /// Tool Center Point (TCP) relative to the Tool's base coordinate system, 
        /// its weight in Kg and its center of gravity. 
        /// In other words, if the Tool gets attached to the robot flange in 
        /// XYZ [0, 0, 0], where is the tooltip and how is it oriented?
        /// </summary>
        /// <param name="name">Tool name</param>
        /// <param name="tcpX">X coordinate of Tool Center Point</param>
        /// <param name="tcpY">Y coordinate of Tool Center Point</param>
        /// <param name="tcpZ">Z coordinate of Tool Center Point</param>
        /// <param name="tcp_vX0">X coordinate of X Axis of Tool Center Point</param>
        /// <param name="tcp_vX1">Y coordinate of X Axis of Tool Center Point</param>
        /// <param name="tcp_vX2">Z coordinate of X Axis of Tool Center Point</param>
        /// <param name="tcp_vY0">X coordinate of Y Axis of Tool Center Point</param>
        /// <param name="tcp_vY1">Y coordinate of Y Axis of Tool Center Point</param>
        /// <param name="tcp_vY2">Z coordinate of Y Axis of Tool Center Point</param>
        /// <param name="weight">Tool weight in Kg</param>
        /// <param name="cogX">X coordinate of Center Of Gravity</param>
        /// <param name="cogY">Y coordinate of Center Of Gravity</param>
        /// <param name="cogZ">Z coordinate of Center Of Gravity</param>
        /// <returns></returns>
        static public Tool2 Create(string name, Point position,
            double[] orient, double weight, double cogX, double cogY, double cogZ)
        {
            return new Tool2(name,
                position, orient, weight,
                cogX, cogY, cogZ);
        }


        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "Tool[\"{0}\", Tip{1}, Orientation{2}, {3} kg]",
                this.name,
                this.TCPPosition,
                this.TCPOrientation,
                this.Weight);
            //this.centerOfGravity);
        }

        /// <summary>
        /// Converts this Tool object to message-compatible instruction.
        /// </summary>
        /// <returns></returns>
        public string ToInstruction()
        {
            //$"Tool.Create(\"{this.name}\", {this.TCPPosition.X}, {this.TCPPosition.Y}, {this.TCPPosition.Z}, {this.TCPOrientation.XAxis.X}, {this.TCPOrientation.XAxis.Y}, {this.TCPOrientation.XAxis.Z}, {this.TCPOrientation.YAxis.X}, {this.TCPOrientation.YAxis.Y}, {this.TCPOrientation.YAxis.Z}, {this.Weight}, {this.CenterOfGravity.X}, {this.CenterOfGravity.Y}, {this.CenterOfGravity.Z});";

            return string.Format(CultureInfo.InvariantCulture,
                "Tool.Create(\"{0}\",{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11});",
                this.name,
                this.TCPPosition.X,
                this.TCPPosition.Y,
                this.TCPPosition.Z,
                this.TCPOrientation[0],
                this.TCPOrientation[1],
                this.TCPOrientation[2],
                this.TCPOrientation[3],
                this.Weight,
                this.CenterOfGravity.X,
                this.CenterOfGravity.Y,
                this.CenterOfGravity.Z);
        }

    }
}
