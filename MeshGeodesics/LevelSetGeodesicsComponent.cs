﻿using System;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;

using Cureos.Numerics.Optimizers;

namespace MeshGeodesics
{
    
    /// <summary>
    /// Mesh geodesics component.
    /// </summary>
    public class MeshGeodesicsComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public MeshGeodesicsComponent()
          : base("Level Sets Geodesics", "LS Geod",
            "Generate geodesics by level set method",
            "Alan", "Geodesic Patterns")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh", GH_ParamAccess.item);
            pManager.AddNumberParameter("Initial Values", "V", "OPTIONAL Initial Values per vertex", GH_ParamAccess.list);
            pManager.AddNumberParameter("Width", "W", "Desired separation between geodesics", GH_ParamAccess.item);
            pManager.AddNumberParameter("Lambda", "lmbd", "Regularization weight", GH_ParamAccess.item);
            pManager.AddNumberParameter("Nu", "nu", "Equal width weight", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Pattern", "P", "Resulting level set curve pattern", GH_ParamAccess.list);
            pManager.AddVectorParameter("Gradient", "grad", "Gradient of scalar field per face", GH_ParamAccess.list);
            pManager.AddNumberParameter("Divergence", "div", "Divergence of the gradient per vertex", GH_ParamAccess.list);
            pManager.AddNumberParameter("Voronoi Area", "A", "Voronoi Area of each vertex of the mesh", GH_ParamAccess.list);
            pManager.AddNumberParameter("MinimizationValue", "Min", "Minimization value", GH_ParamAccess.item);
            pManager.AddNumberParameter("Function values", "Values", "Resulting scalar function values", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = new Mesh();
            List<double> initialValues = new List<double>();
            double desiredWidth = 0.0;
            double lambda = 0.0;
            double nu = 0.0;

            if (!DA.GetData(0, ref mesh)) return;
            if (!DA.GetDataList(1, initialValues)) return;
            if (!DA.GetData(2, ref desiredWidth)) return;
            if (!DA.GetData(3, ref lambda)) return;
            if (!DA.GetData(4, ref nu)) return;


            mesh.FaceNormals.ComputeFaceNormals();

            GeodesicsFromLevelSets levelSets = new GeodesicsFromLevelSets(mesh, initialValues, desiredWidth, lambda, nu);

            var optimizer = new Bobyqa(mesh.Vertices.Count, levelSets.Compute);
            optimizer.MaximumFunctionCalls = 200;

            double[] x = initialValues.ToArray();
            var result = optimizer.FindMinimum(x);
            List<double> finalValues = new List<double>(result.X);  

            List<double> divergence = levelSets.ComputeDivergence(false);

            DA.SetDataList(1, levelSets.Gradient);
            DA.SetDataList(2, divergence);
            DA.SetDataList(3, levelSets.VertexVoronoiArea);
            DA.SetData(4, result.F);
            DA.SetDataList(5, finalValues);

        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("c9253440-065f-438e-9c54-1c45f82085f3"); }
        }
    }
}
