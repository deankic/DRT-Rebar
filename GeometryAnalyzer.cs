using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Revit.Async.ExternalEvents;
using System;
using System.Collections.Generic;
using System.Linq;


namespace DRT.core
{


    public class GeometryAnalyzerAsync : SyncGenericExternalEventHandler<string, Definition>
    {
        public override object Clone()
        {
            return new GeometryAnalyzerAsync();
        }

        public override string GetName()
        {
            return "create All Schedules";
        }

        protected override Definition Handle(UIApplication app, string parameter)
        {
            GeometryAnalyzer createAssemblies = new GeometryAnalyzer();
            createAssemblies.Start(app);

            return null;
        }
    }



    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GeometryAnalyzer : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Start(commandData.Application);
        }

        public Result Start(UIApplication app)

        {
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;
            var selection = uidoc.Selection;

            // Prompts the user to select a single element of type Floor
            Reference pickedRef = selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, "Select the source beam.");
            Element srcElement = doc.GetElement(pickedRef);


            ElementClassFilter rebarFilter = new ElementClassFilter(typeof(Rebar));

            var rebarIds = srcElement.GetDependentElements(rebarFilter);


            // Verify if the selected element can host rebar
            if (!IsElementValidForRebar(srcElement))
            {
                TaskDialog.Show("Error", "Selected element cannot host rebar.");
                return Result.Failed;
            }


            Reference targetRef = selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, "Select the target beam.");
            Element targetElement = doc.GetElement(targetRef);

            if (!IsElementValidForRebar(targetElement))
            {
                TaskDialog.Show("Error", "Selected target element cannot host rebar.");
                return Result.Failed;
            }

            CopyRebarToTarget(doc, rebarIds, srcElement, targetElement);

            // column
            // get the base of the column and the column and all faces
            // ()
            // add rebar to top face and (sqquare bars
            // )
            // straight bars to bottom 
            // check the embedment of the straight bars 

            // devide column in to thirds to make spacing of square bars closer to ends and furtherin mid spanb

            // can the analytical model show forces and moments.
            // create a template that can be used for diffrent conditions and check the conditions()




            // beam





            // slab
            // get the general shape of the slab (top should be same as bottom )
            // check for any openings
            // check for any angles faces or arcs 





            return Result.Succeeded;

        }


        private bool IsElementValidForRebar(Element element)
        {
            // Implement logic to verify if the element can host rebar
            return true; // Placeholder
        }


        private void CopyRebarToTarget(Document doc, IList<ElementId> rebarIds, Element srcElement, Element targetElement)
        {
            using (Transaction trans = new Transaction(doc, "Copy Rebar"))
            {
                trans.Start();

                // Assuming the Rebar creation method requires curve, type, element, and document.
                // You may need to adjust this logic based on how you want to copy the rebar,
                // including handling its shape, parameters, and constraints specifically.

                // Get geometry for source and target elements to find matching faces or positions for constraints
                // This is a placeholder: actual implementation will depend on how you map source to target constraints
                GeometryElement srcGeometry = srcElement.get_Geometry(new Options());

                GeometryElement tgtGeometry = targetElement.get_Geometry(new Options());

                foreach (ElementId rebarId in rebarIds)
                {
                    Rebar rebar = doc.GetElement(rebarId) as Rebar;
                    if (rebar != null)
                    {

                        // Example: Copy the rebar directly without adjusting its shape or constraints
                        // Note: This simplistic approach may not directly apply; you'll likely need to
                        // modify the rebar geometry to match the target element


                       // rebar.SetHostId(doc, targetElement.Id);

                        //rebar.

                        var match = FaceMatcher.GetEquivalentFaces(srcElement, targetElement);


                        Console.WriteLine();
                        // Now adjust the constraints of the copied rebar to match the target element's geometry
                        // This requires specific API calls to manipulate the Rebar's constraints, which
                        // are not directly illustrated here due to API complexity and variability in requirements

                        // Example pseudo-code for constraint adjustment (not actual API code):
                        // Rebar copiedRebar = doc.GetElement(copiedRebarId) as Rebar;
                        // AdjustRebarConstraints(copiedRebar, srcGeometry, tgtGeometry);

                        // Actual constraint adjustment code will depend on your specific requirements
                        // and the details of the Rebar and Element geometry.
                    }
                }



                trans.Commit();
            }
        }


    }


    public class FaceMatcher
    {
        public static Dictionary<Face, Face> GetEquivalentFaces(Element srcElement, Element targetElement)
        {
            var srcFaces = ExtractFaces(srcElement);
            var targetFaces = ExtractFaces(targetElement);

            // Dictionary to hold matching faces
            var matchedFaces = new Dictionary<Face, Face>();

            foreach (var srcFace in srcFaces)
            {
                var srcFaceNormal = GetFaceNormal(srcFace);
                var srcFaceArea = GetFaceArea(srcFace);

                foreach (var targetFace in targetFaces)
                {
                    var targetFaceNormal = GetFaceNormal(targetFace);
                    var targetFaceArea = GetFaceArea(targetFace);

                    // Check if faces are equivalent; this is a simplistic check and may need refinement
                    if (FacesAreEquivalent(srcFaceNormal, srcFaceArea, targetFaceNormal, targetFaceArea))
                    {
                        matchedFaces[srcFace] = targetFace;
                        break; // Assuming one-to-one match, move to the next source face
                    }
                }
            }

            return matchedFaces;
        }

        private static List<Face> ExtractFaces(Element element)
        {
            var options = new Options();
            GeometryElement geometryElement = element.get_Geometry(options);
            List<Face> faces = new List<Face>();

            // Iterate through geometry instances
            foreach (GeometryObject geomObj in geometryElement)
            {
                if (geomObj is GeometryInstance geometryInstance)
                {
                    GeometryElement instanceGeometry = geometryInstance.GetInstanceGeometry();

                    // Iterate through solids in the geometry instance
                    foreach (GeometryObject instanceGeomObj in instanceGeometry)
                    {
                        if (instanceGeomObj is Solid solid && solid.Faces.Size > 0)
                        {
                            foreach (Face face in solid.Faces)
                            {
                                faces.Add(face);
                            }
                        }
                    }
                }
            }

            return faces;
        }

        private static XYZ GetFaceNormal(Face face)
        {
            UV uv = new UV((face.GetBoundingBox().Min.U + face.GetBoundingBox().Max.U) / 2,
                           (face.GetBoundingBox().Min.V + face.GetBoundingBox().Max.V) / 2);
            return face.ComputeNormal(uv);
        }

        private static double GetFaceArea(Face face)
        {
            // Implement logic to compute the area of the face
            return face.Area;
        }

        private static bool FacesAreEquivalent(XYZ srcNormal, double srcArea, XYZ targetNormal, double targetArea)
        {
            // Define tolerances for comparison
            double normalTolerance = 0.01; // Tolerance for normal vector direction difference
            double areaTolerance = 0.1; // Tolerance for area difference

            // Check if normals are equivalent (i.e., their difference is within the tolerance)
            bool normalsEquivalent = (srcNormal.Normalize().Subtract(targetNormal.Normalize())).GetLength() < normalTolerance;

            // Check if areas are equivalent (i.e., their difference is within the tolerance)
            bool areasEquivalent = Math.Abs(srcArea - targetArea) < areaTolerance;

            // Return true if both normals and areas are equivalent
            return normalsEquivalent && areasEquivalent;
        }
    }



}
