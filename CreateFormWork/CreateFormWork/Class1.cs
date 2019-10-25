using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Architecture;
using System.Collections;


namespace CreateFormWork
{
    [Transaction(TransactionMode.Manual)]
    //手动事务
    public class FirstCreateFormWork : IExternalCommand
    {

        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;
            TaskDialog.Show("选择", "请选择柱");
            Selection se = uiDoc.Selection;
            FaceArray faces;
            if (se == null)
            {
                TaskDialog.Show("选择", "未进行选择");
                return Result.Failed;
            }
            else
            {
                Reference re = se.PickObject(ObjectType.Element);
                Element col = doc.GetElement(re);
                faces = GetFaceArray(col);

            }
            Face bottomFace = null;
            if (faces != null)
            {
                //取得底面
                foreach (Face face in faces)
                {
                    if (face.ComputeNormal(new UV()).Z == -1)//判断法向量 z=1
                    {
                        if (bottomFace == null)
                        {
                            bottomFace = face;
                        }
                        else
                        {
                            if (bottomFace.EdgeLoops.get_Item(0).get_Item(0).AsCurve().GetEndPoint(0).Z > face.EdgeLoops.get_Item(0).get_Item(0).AsCurve().GetEndPoint(0).Z)
                            {
                                bottomFace = face;
                            }
                        }
                    }
                }
            }
            IList<CurveLoop> bottomEdgesCurveLoop = bottomFace.GetEdgesAsCurveLoops();
            //底面点集合
            List<XYZ> bottumPoint = new List<XYZ>();
            //底面CurveLoop
            CurveLoop bottumCurveLoop = bottomEdgesCurveLoop[0];
            //通过偏移得到模板线
            CurveLoop formWorkCurveLoop = CurveLoop.CreateViaOffset(bottumCurveLoop, 0.0590551, new XYZ(0, 0, -1));
            using (Transaction transaction = new Transaction(doc, "start"))
            {
                transaction.Start();
                foreach (Curve curve in formWorkCurveLoop)
                {
                    Wall wall = Wall.Create(doc, curve, new ElementId(311), true);

                }
                transaction.Commit();
            }


            ///下面这些是通过获得点来计算模板轮廓的算法尸体

            //foreach (Curve curve in bottumCurveLoop)
            //{
            //    bottumPoint.Add(curve.GetEndPoint(0));
            //}
            //double cX = 0, cY = 0, cZ = 0;
            //foreach(XYZ xYZ in bottumPoint)
            //{
            //    cX += xYZ.X;
            //    cY += xYZ.Y;
            //    cZ += xYZ.Z;
            //}
            //EdgeArray bottomEdges = bottomFace.EdgeLoops.get_Item(0);
            //double cX = 0, cY = 0, cZ = 0;
            //if (bottomEdges.Size == 4)
            //{
            //    foreach(Edge edge in bottomEdges)
            //    {
            //        cX = cX + edge.AsCurve().GetEndPoint(0).X;
            //        cY = cY + edge.AsCurve().GetEndPoint(0).Y;
            //        cZ = cZ + edge.AsCurve().GetEndPoint(0).Z;
            //        cX = cX + edge.AsCurve().GetEndPoint(1).X;
            //        cY = cY + edge.AsCurve().GetEndPoint(1).Y;
            //        cZ = cZ + edge.AsCurve().GetEndPoint(1).Z;
            //    }

            //}
            //XYZ centerPoint = new XYZ(cX / 4, cY / 4, cZ / 4);
            ///上面这些是通过获得点来计算模板轮廓的算法尸体

            return Result.Succeeded;
        }
        public FaceArray GetFaceArray(Element ele)
        {
            //从Element中获取的face集合
            if (ele == null)
            {
                return null;
            }
            else
            {
                GeometryElement geometryElement = ele.get_Geometry(new Options());
                FaceArray faceArray = new FaceArray();
                //非系统族有两侧嵌套，系统族是一层
                //参考 https://blog.csdn.net/liyazhen2011/article/details/88737014
                foreach (GeometryObject geoObject in geometryElement)
                {
                    GeometryInstance instance = geoObject as GeometryInstance;
                    if (instance == null)
                        continue;
                    GeometryElement geoElement = instance.GetInstanceGeometry();
                    if (geometryElement == null)
                        continue;
                    foreach (GeometryObject elem in geoElement)
                    {
                        Solid solid = elem as Solid;
                        if (solid == null || solid.Volume.ToString() == "0")
                            continue;
                        faceArray = solid.Faces;
                    }

                }
                return faceArray;
            }



        }
    }
}
