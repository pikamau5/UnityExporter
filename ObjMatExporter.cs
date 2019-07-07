/*
Based on ObjExporter.cs and http://answers.unity3d.com/questions/317951/how-to-use-editorobjexporter-obj-saving-script-fro.html.

Usage:
	- Unity editor script - 
	Export selected objects + textures to a folder, drag & drop exported file to 3ds max and keep correct material setups etc.

Export multiple meshes to one obj! (see original script)

by: Laura Koekoek | laurakoekoek91@gmail.com

*/

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;


struct ObjMaterial
{
	public string name;

	//albedo name
	public string textureName;

	//additional texture maps
	public string textureNameBump;
	public string textureNameOcclusion;
	public string textureNameMetalGloss;
	public string textureNameEmission;

	//102
	public Texture2D tex1;
	public Texture2D texAlbedo;
	public Texture2D texBump;
	public Texture2D texOcclusion;
	public Texture2D texEmission;

	public string materialName;

}

public class ObjMatExporter : ScriptableObject
{
	private static int vertexOffset = 0;
	private static int normalOffset = 0;
	private static int uvOffset = 0;

	public static int objectCounter = 0;

	public static bool mainNull = false;
	public static bool occlusionNull = false;
	public static bool metalGlossNull = false;
	public static bool bumpNull = false;
	public static bool emissiveNull = false;

	// Generic target folder
	private static string targetFolder = "MeshExporter";
	private static string flatsFolder = "assets/flats";
	private static string mtlFolder = targetFolder + "/mtl";

	// for explorer path conversion
	private static string explorerPath;

	//progressbar counters
	private static int n;
	private static int nCount;
	private static float progressBar;

	private static string singleFileName = "UnityExport";
	private static string maxFilterStringChar = "#"; // using this for max material name parsing...
	private static String[] uniqueNameComparison;

	private static string MeshToString(MeshFilter mf, Dictionary<string, ObjMaterial> materialList) 
	{
		

		if (n != 0) {
			progressBar = (float)n/(float)nCount;
		}
		EditorUtility.DisplayProgressBar ("Obj Exporter", "Exporting object " + ++n +" of "+ nCount +" - " + mf.name , progressBar);

		Mesh m = mf.sharedMesh;
		Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
		
		StringBuilder sb = new StringBuilder();
		
		//sb.Append("g ").Append(mf.name).Append("\n");

		// Ignore meshes with <4 vertices
		if (m == null || m.vertices == null || m.vertexCount < 4) { // ???vertex index error???
			Debug.Log (mf.gameObject.name + " missing");
			return "";
		}



		foreach(Vector3 lv in m.vertices) 
		{
			Vector3 wv = mf.transform.TransformPoint(lv);
			
			//This is sort of ugly - inverting x-component since we're in
			//a different coordinate system than "everyone" is "used to".
			sb.Append(string.Format("v {0} {1} {2}\n",-wv.x,wv.y,wv.z));
		}
		sb.Append("\n");
		
		foreach(Vector3 lv in m.normals) 
		{
			Vector3 wv = mf.transform.TransformDirection(lv);
			
			sb.Append(string.Format("vn {0} {1} {2}\n",-wv.x,wv.y,wv.z));
		}
		sb.Append("\n");
		
		foreach(Vector3 v in m.uv) 
		{
			sb.Append(string.Format("vt {0} {1}\n",v.x,v.y));
		}
		
		for (int material=0; material < m.subMeshCount; material ++) {
			sb.Append("\n");
			sb.Append("g ").Append(mf.name).Append("\n");
			sb.Append("usemtl ").Append(mats[material].name).Append("\n");
			sb.Append("usemap ").Append(mats[material].name).Append("\n");
			
			//See if this material is already in the materiallist.
			try
			{
				
				ObjMaterial objMaterial = new ObjMaterial();



				objMaterial.name = mats[material].name;
				
				if (mats[material].mainTexture && mats[material].shader.name == "Standard") {





					// getting texture maps from shader
					objMaterial.textureName = EditorUtility.GetAssetPath(mats[material].mainTexture);

					objMaterial.textureNameBump = EditorUtility.GetAssetPath(mats[material].GetTexture("_BumpMap"));
					objMaterial.textureNameOcclusion = EditorUtility.GetAssetPath(mats[material].GetTexture("_OcclusionMap"));



					// obsolete?
					objMaterial.textureNameMetalGloss = EditorUtility.GetAssetPath(mats[material].GetTexture("_MetallicGlossMap"));



					// GETTING THE TEXTURE MAPS
					// Albedo & transparency(?)
					objMaterial.texAlbedo = new Texture2D(512,512,TextureFormat.RGBA32, false);
					objMaterial.texAlbedo = mats[material].GetTexture("_MainTex") as Texture2D;

					// Bump
					objMaterial.texBump = new Texture2D(512,512,TextureFormat.RGBA32, false);
					objMaterial.texBump = mats[material].GetTexture("_BumpMap") as Texture2D;

					// Bump
					objMaterial.texOcclusion = new Texture2D(512,512,TextureFormat.RGBA32, false);
					objMaterial.texOcclusion = mats[material].GetTexture("_OcclusionMap") as Texture2D;

					// MetalGloss
					objMaterial.tex1 = new Texture2D(512,512,TextureFormat.RGBA32, false);
					objMaterial.tex1 = mats[material].GetTexture("_MetallicGlossMap") as Texture2D;

					// EmissiveMap
					objMaterial.texEmission = new Texture2D(512,512,TextureFormat.RGBA32, false);
					objMaterial.texEmission = mats[material].GetTexture("_OcclusionMap") as Texture2D;



					objMaterial.materialName = mats[material].name;

					objMaterial.textureNameEmission = EditorUtility.GetAssetPath(mats[material].GetTexture("_EmissionMap"));

					/* these are in wrong place
					// check missing textures (this could be done more efficiently, but the current check didn't work)
					if (mats[material].GetTexture("_MainTex") == null) { mainNull = true; }else  { mainNull = false; }
					if (mats[material].GetTexture("_BumpMap") == null) { bumpNull = true; }else  { bumpNull = false; }
					if (mats[material].GetTexture("_OcclusionMap") == null) { occlusionNull = true; }else  { occlusionNull = false; }
					if (mats[material].GetTexture("_MetallicGlossMap") == null) { metalGlossNull = true; }else  { metalGlossNull = false; }
					if (mats[material].GetTexture("_EmissionMap") == null) { emissiveNull = true; }else  { emissiveNull = false; }
					*/
				}
				else {
					objMaterial.textureName = null;
					Debug.Log("only supports std shader - " + objMaterial.name);
				}
				
				materialList.Add(objMaterial.name, objMaterial);
			}
			catch (ArgumentException)
			{
				//Already in the dictionary
			}
			
			
			int[] triangles = m.GetTriangles(material);
			for (int i=0;i<triangles.Length;i+=3) 
			{
				//Because we inverted the x-component, we also needed to alter the triangle winding.
				sb.Append(string.Format("f {1}/{1}/{1} {0}/{0}/{0} {2}/{2}/{2}\n", 
				                        triangles[i]+1 + vertexOffset, triangles[i+1]+1 + normalOffset, triangles[i+2]+1 + uvOffset));
			}
		}
		
		vertexOffset += m.vertices.Length;
		normalOffset += m.normals.Length;
		uvOffset += m.uv.Length;
		
		return sb.ToString();
	}
	
	private static void Clear()
	{
		vertexOffset = 0;
		normalOffset = 0;
		uvOffset = 0;
	}
	
	private static Dictionary<string, ObjMaterial> PrepareFileWrite()
	{
		Clear();
		
		return new Dictionary<string, ObjMaterial>();
	}
	
	private static void MaterialsToFile(Dictionary<string, ObjMaterial> materialList, string folder, string filename)
	{
		//Uncomment brackets to disable mtl export
		///*
		using (StreamWriter sw = new StreamWriter(mtlFolder + "/" + filename + ".mtl")) 
		{
			foreach( KeyValuePair<string, ObjMaterial> kvp in materialList )
			{
				sw.Write("\n");
				sw.Write("newmtl {0}\n", kvp.Key);

				// Material constants ignored for now...
				//sw.Write("Ka  0.6 0.6 0.6\n");
				//sw.Write("Kd  0.6 0.6 0.6\n");
				//sw.Write("Ks  0.9 0.9 0.9\n");
				//sw.Write("d  1.0\n");
				//sw.Write("Ns  0.0\n");

				sw.Write("illum 2\n");

				// EXPORTING TEXTURE MAPS & WRITING TO MTL FILE
				// Albedo texture

				// Creating folder for material
				System.IO.Directory.CreateDirectory(targetFolder + "/" + flatsFolder + "/" + kvp.Value.materialName);


				string fullPathMaterial = targetFolder + "/" + flatsFolder + "/" + kvp.Value.materialName;
				string relativePathMaterial = flatsFolder + "/" + kvp.Value.materialName;

				// get missing textures for each object
				if (kvp.Value.textureName == null || kvp.Value.textureName == "") { mainNull = true; }else  { mainNull = false; }
				if (kvp.Value.textureNameBump == null || kvp.Value.textureNameBump == "") { bumpNull = true; }else  { bumpNull = false; }
				if (kvp.Value.textureNameEmission == null || kvp.Value.textureNameEmission == "") { emissiveNull = true; }else  { emissiveNull = false; }
				if (kvp.Value.textureNameMetalGloss == null || kvp.Value.textureNameMetalGloss == "") { metalGlossNull = true; }else  { metalGlossNull = false; }
				if (kvp.Value.textureNameOcclusion == null || kvp.Value.textureNameOcclusion == "") { occlusionNull = true; }else  { occlusionNull = false; }

			



				if (!mainNull) 
				{
					// Allow Read/Write
					TextureImporter textureImporter = AssetImporter.GetAtPath (kvp.Value.textureName) as TextureImporter;
					textureImporter.isReadable = true;



					TextureImporterPlatformSettings settings = textureImporter.GetDefaultPlatformTextureSettings();
					settings.format = TextureImporterFormat.RGBA32;
					textureImporter.SetPlatformTextureSettings (settings);


					AssetDatabase.ImportAsset (kvp.Value.textureName);
					
					byte[] png0 = kvp.Value.texAlbedo.EncodeToPNG ();
					string fullPathAlbedo = fullPathMaterial + "/" + kvp.Value.materialName + "_Albedo" +  ".png"; // pls simplify this... 
					string relativePathAlbedo = relativePathMaterial + "/" + kvp.Value.materialName + "_Albedo" +  ".png";

					File.WriteAllBytes (fullPathAlbedo,png0);

					sw.Write("map_Kd {0}", relativePathAlbedo);

				}


				// Normal texture
				if (!bumpNull)
				{
					

					// Allow Read/Write
					TextureImporter textureImporter = AssetImporter.GetAtPath (kvp.Value.textureNameBump) as TextureImporter;
					textureImporter.isReadable = true;
					TextureImporterPlatformSettings settings = textureImporter.GetDefaultPlatformTextureSettings();
					settings.format = TextureImporterFormat.RGBA32;
					textureImporter.SetPlatformTextureSettings (settings);
					bool textureWasBump = false;

					if (textureImporter.textureType == TextureImporterType.NormalMap) {
						textureWasBump = true;
						textureImporter.textureType = TextureImporterType.Default;
					}

						
					AssetDatabase.ImportAsset (kvp.Value.textureNameBump);

					byte[] pngBump = kvp.Value.texBump.EncodeToPNG ();
					string fullPathBump = fullPathMaterial + "/" + kvp.Value.materialName + "_Normal" +  ".png"; // pls simplify this... 
					string relativePathBump = relativePathMaterial + "/" + kvp.Value.materialName + "_Normal" +  ".png";

					File.WriteAllBytes (fullPathBump,pngBump);

					// change the texture type back
					if (textureWasBump) {
						textureImporter.textureType = TextureImporterType.NormalMap;
						AssetDatabase.ImportAsset (kvp.Value.textureNameBump);
					}
					
					sw.Write("\n");
					sw.Write("map_bump {0}", relativePathBump);
				}

				// OcclusionMap
				if (!occlusionNull)
				{
					// Allow Read/Write
					TextureImporter textureImporter = AssetImporter.GetAtPath (kvp.Value.textureNameOcclusion) as TextureImporter;
					textureImporter.isReadable = true;
					TextureImporterPlatformSettings settings = textureImporter.GetDefaultPlatformTextureSettings();
					settings.format = TextureImporterFormat.RGBA32;
					textureImporter.SetPlatformTextureSettings (settings);
					AssetDatabase.ImportAsset (kvp.Value.textureNameOcclusion);

					byte[] pngOcclusion = kvp.Value.texOcclusion.EncodeToPNG ();
					string fullPathOcclusion = fullPathMaterial + "/" + kvp.Value.materialName + "_Occlusion" +  ".png"; // pls simplify this... 
					string relativePathOcclusion = relativePathMaterial + "/" + kvp.Value.materialName + "_Occlusion" +  ".png";

					File.WriteAllBytes (fullPathOcclusion,pngOcclusion);


					sw.Write("\n");
					sw.Write("map_Ka {0}", relativePathOcclusion);
				}


				// MetalGlossMap
				if (!metalGlossNull)
				{
					// Allow Read/Write
					TextureImporter textureImporter = AssetImporter.GetAtPath (kvp.Value.textureNameMetalGloss) as TextureImporter;
					textureImporter.isReadable = true;
					TextureImporterPlatformSettings settings = textureImporter.GetDefaultPlatformTextureSettings();
					settings.format = TextureImporterFormat.RGBA32;
					textureImporter.SetPlatformTextureSettings (settings);
					AssetDatabase.ImportAsset (kvp.Value.textureNameMetalGloss);

					//Converting Metalness map, removing alpha
					Color[] pixelsColor = kvp.Value.tex1.GetPixels();
					Color[] resultingPixels = new Color[pixelsColor.Length];
					for (int c=0;c<pixelsColor.Length;c++) {
						resultingPixels[c] = new Color(pixelsColor[c].r, pixelsColor[c].g, pixelsColor[c].b, 1.0f);
					}
					Texture2D result = new Texture2D(kvp.Value.tex1.width, kvp.Value.tex1.height);
					result.SetPixels(resultingPixels);
					result.Apply();

					byte[] png1 = result.EncodeToPNG ();
					//write rgb
					string fullPathMetal = fullPathMaterial + "/" + kvp.Value.materialName + "_Metalness" +  ".png"; // pls simplify this... 
					string relativePathMetal = relativePathMaterial + "/" + kvp.Value.materialName + "_Metalness" +  ".png";

					File.WriteAllBytes (fullPathMetal,png1);


					//Converting Smoothness to roughness

					pixelsColor = kvp.Value.tex1.GetPixels();
					resultingPixels = new Color[pixelsColor.Length];
					for (int c=0;c<pixelsColor.Length;c++) {
						resultingPixels[c] = new Color(1-pixelsColor[c].a, 1-pixelsColor[c].a, 1-pixelsColor[c].a, 1.0f);
					}
					result = new Texture2D(kvp.Value.tex1.width, kvp.Value.tex1.height);
					result.SetPixels(resultingPixels);
					result.Apply();

					byte[] png2 = result.EncodeToPNG ();
					//write rgb

					string fullPathRough = fullPathMaterial + "/" + kvp.Value.materialName + "_Roughness" + ".png";
					string relativePathRough = relativePathMaterial + "/" + kvp.Value.materialName + "_Roughness" +  ".png";

					File.WriteAllBytes (fullPathRough,png2);

					sw.Write("\n");
					sw.Write("map_Ks {0}", relativePathMetal);

					sw.Write("\n");
					//temporarily hijacking emissive
					sw.Write("map_refl {0}", relativePathRough);
				}

				/*
				// Emission map
				if (!emissiveNull)
				{
					// Allow Read/Write
					TextureImporter textureImporter = AssetImporter.GetAtPath (kvp.Value.textureNameEmission) as TextureImporter;
					textureImporter.isReadable = true;
					TextureImporterPlatformSettings settings = textureImporter.GetDefaultPlatformTextureSettings();
					settings.format = TextureImporterFormat.RGBA32;
					textureImporter.SetPlatformTextureSettings (settings);
					AssetDatabase.ImportAsset (kvp.Value.textureNameEmission);


					byte[] pngEmission = kvp.Value.texEmission.EncodeToPNG ();
					string fullPathEmission = fullPathMaterial + "/" + kvp.Value.materialName + "_Emissive" +  ".png"; // pls simplify this... 
					string relativePathEmission = relativePathMaterial + "/" + kvp.Value.materialName + "_Emissive" +  ".png";

					File.WriteAllBytes (fullPathEmission,pngEmission);


					sw.Write("\n");
					sw.Write("map_d {0}", relativePathEmission);

				}

				*/
				sw.Write("\n\n\n");
			}
		} //*/
	}

	private static void MeshToFile(MeshFilter mf, string folder, string filename) 
	{
		

		Dictionary<string, ObjMaterial> materialList = PrepareFileWrite();
		
		using (StreamWriter sw = new StreamWriter(folder +"/" + filename +  ".obj")) 
		{
			if (mf.GetComponent<Renderer> ().sharedMaterials.Length == 1) {
				sw.Write ("mtllib ./" + "mtl/" + filename + ".mtl\n");
				sw.Write (MeshToString (mf, materialList));
			} else {
			
				Debug.Log ("Multisub materials not supported! (" + mf.name + ")");
			
			}

		}
		
		MaterialsToFile(materialList, folder, filename);
	}
	
	private static void MeshesToFile(MeshFilter[] mf, string folder, string filename) 
	{
		Dictionary<string, ObjMaterial> materialList = PrepareFileWrite();
		
		using (StreamWriter sw = new StreamWriter(folder +"/" + filename + ".obj")) 
		{
			sw.Write("mtllib ./" + "mtl/" + filename + ".mtl\n");
			
			for (int i = 0; i < mf.Length; i++)
			{
				sw.Write(MeshToString(mf[i], materialList));


			}
		}
		
		MaterialsToFile(materialList, folder, filename);
	}
	
	private static bool CreateTargetFolder()
	{
		try
		{
			System.IO.Directory.CreateDirectory(targetFolder);
			System.IO.Directory.CreateDirectory(targetFolder + "/" + flatsFolder);
			System.IO.Directory.CreateDirectory(mtlFolder);
		}
		catch
		{
			EditorUtility.DisplayDialog("Error!", "Failed to create target folder!", "");
			return false;
		}
		
		return true;
	}

	[MenuItem ("Custom/Obj Exporter - Export selection to single obj file")]
	static void ExportWholeSelectionToSingle()
	{
		if (!CreateTargetFolder())
			return;


		Transform[] selection = Selection.GetTransforms(SelectionMode.Editable | SelectionMode.ExcludePrefab);

		// progressbar stuff
		n = 0;
		nCount = selection.Length;
		progressBar = 0;

		if (selection.Length == 0)
		{
			EditorUtility.DisplayDialog("No source object selected!", "Please select one or more target objects", "");
			return;
		}

		int exportedObjects = 0;

		// adding this number to mesh names to prevent duplicate naming error in max
		int meshfilterCounter = 0;

		ArrayList mfList = new ArrayList();

		for (int i = 0; i < selection.Length; i++)
		{
			uniqueNameComparison = new String[selection.Length];

			//checking for non unique names
			for (int h = 0; h < selection.Length; h++) {
				uniqueNameComparison [h] = selection[h].name;
			}
			uniqueNameComparison [i] = null; // ignore own object name

			for (int z = 0; z < uniqueNameComparison.Length; z++) {
				
				if (selection [i].name == uniqueNameComparison [z]) {
					Debug.Log ("Non-unique name found! " + selection[i].name);

					if (EditorUtility.DisplayDialog("Obj Exporter", "Duplicate name found: " + selection[i].name + "\nThis will cause errors.","Cancel export","Fix (Add postfix)")){
						return;
					}

					// adding postfix to duplicate object
					selection [i].name = selection[i].name + "_" + meshfilterCounter;
					meshfilterCounter++;
				}
			}



			Component[] meshfilter = selection[i].GetComponentsInChildren(typeof(MeshFilter));

			for (int m = 0; m < meshfilter.Length; m++)
			{
				
				exportedObjects++;
				mfList.Add(meshfilter[m]);
			}
		}


		if (exportedObjects > 0) {
			MeshFilter[] mf = new MeshFilter[mfList.Count];

			for (int i = 0; i < mfList.Count; i++) {
				mf [i] = (MeshFilter)mfList [i];
			}

			string filename = EditorSceneManager.GetActiveScene ().name + "_" + exportedObjects;

			int stripIndex = filename.LastIndexOf (Path.PathSeparator);

			if (stripIndex >= 0)
				filename = filename.Substring (stripIndex + 1).Trim ();

			MeshesToFile (mf, targetFolder, singleFileName + maxFilterStringChar); //...


			if (EditorUtility.DisplayDialog ("Obj Exporter", "Exported " + exportedObjects + " objects.", "Open Folder in Explorer", "Ok."))
			{
				// opening folder in explorer
				explorerPath = Application.dataPath + "/" + targetFolder; 
				explorerPath = explorerPath.Replace (@"/" + "Assets", ""); // Explorer is weird
				explorerPath = explorerPath.Replace (@"/", @"\");   // explorer doesn't like front slashes

				if (Directory.Exists (explorerPath))
				{
					System.Diagnostics.Process.Start ("explorer.exe", explorerPath);
				}
				else
				{
					Debug.Log ("directory " + explorerPath + "  not found!");
				}
			}
		}
		else
		{
			EditorUtility.DisplayDialog ("Objects not exported", "Make sure at least some of your selected objects have mesh filters!", "");
		}
		EditorUtility.ClearProgressBar();
	}

	// main
	[MenuItem ("Custom/(OBSOLETE) Obj Exporter - Export selection to separate obj files")]
	static void ExportSelectionToSeparate()
	{
		

		if (!CreateTargetFolder())
			return;
		
		Transform[] selection = Selection.GetTransforms(SelectionMode.Editable | SelectionMode.ExcludePrefab);

		n = 0; // progressbar object counter
		nCount = selection.Length;
		progressBar = 0;

		if (selection.Length == 0)
		{
			EditorUtility.DisplayDialog("No source object selected!", "Please select one or more target objects", "");
			return;
		}
		
		int exportedObjects = 0;

		// adding this number to mesh names to prevent duplicate naming error in max
		int meshfilterCounter = 0;

		for (int i = 0; i < selection.Length; i++)
		{
			Component[] meshfilter = selection[i].GetComponentsInChildren(typeof(MeshFilter));



			for (int m = 0; m < meshfilter.Length; m++)
			{
				exportedObjects++;

				/*
				// adding this number to mesh names to prevent duplicate naming error in max unique

				*/
				/*'
				string meshfilterTempName = meshfilter[m].name;
				foreach (MeshFilter meshObject in meshfilter) {
				
					// Finding duplicate names...
					for (int n = 0; i < meshfilter.Length; i++) {
					

						if (meshObject.name == meshfilter [n].name) { // ERROR! its not ignoring its own name!!

							//...

								if (EditorUtility.DisplayDialog ("Obj Exporter", "Duplicate name found for object: " + meshObject.name + "\n this might affect object replacing in max", "Select objects", "Automatically fix names (dangerous)")) {
									Selection.activeObject = null;
									Selection.activeGameObject = meshObject.gameObject;
									//return;
								} else {
								
									meshfilter [m].name = meshfilter [m].name + "_" + meshfilterCounter;
									meshfilterCounter++;
								}


						}
					}
				}

				*/
				MeshToFile((MeshFilter)meshfilter[m], targetFolder, selection[i].name + "_" + i + "_" + m);


				//return name to original in editor
				//meshfilter[m].name = meshfilterTempName;


			}
		}

		EditorUtility.ClearProgressBar();

		if (exportedObjects > 0) {
			if (EditorUtility.DisplayDialog ("Obj Exporter", "Exported " + exportedObjects + " objects.", "Open Folder in Explorer", "Ok.")) {


				// opening folder in explorer
				explorerPath = Application.dataPath + "/" + targetFolder; 
				explorerPath = explorerPath.Replace (@"/"+"Assets", ""); // Explorer is weird
				explorerPath = explorerPath.Replace(@"/", @"\");   // explorer doesn't like front slashes

				if (Directory.Exists(explorerPath)) {
					System.Diagnostics.Process.Start("explorer.exe", explorerPath);
				}
				else {Debug.Log("directory " +explorerPath+ "  not found!");
				}
			}




		} else {
			EditorUtility.DisplayDialog ("Objects not exported", "Make sure at least some of your selected objects have mesh filters!", "");
		}
	}




}
