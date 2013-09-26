using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.IO;


[InitializeOnLoad]
internal class Review
{
	const string kEditPostfix = "-edit", kPlayPostfix = "-play";


	static Review ()
	{
		EditorApplication.playmodeStateChanged -= OnPlaymodeSwitch;
		EditorApplication.playmodeStateChanged += OnPlaymodeSwitch;
	}


	static void OnPlaymodeSwitch ()
	{
		string current = CurrentLayoutName, next = "";

		if (EditorApplication.isPlaying)
		{
			if (!current.EndsWith (kEditPostfix))
			{
				return;
			}

			next = current.Substring (0, current.Length - kEditPostfix.Length) + kPlayPostfix;
		}
		else
		{
			if (!current.EndsWith (kPlayPostfix))
			{
				return;
			}

			next = current.Substring (0, current.Length - kPlayPostfix.Length) + kEditPostfix;
		}

		string path = "";

		if (GetValidLayoutPath (next, ref path))
		{
			if (LoadWindowLayout (path))
			{
				CurrentLayoutName = next;
			}
			else
			{
				Debug.LogError ("Failed to load layout at " + next);
			}
		}
		else
		{
			Debug.LogWarning ("Counterpart layout not found: " + next);
		}
	}


	static bool NewAPI
	{
		get
		{
			string[] digits = Application.unityVersion.Split ('.');

			return int.Parse (digits[0]) > 4 || int.Parse (digits[1]) > 2;
		}
	}


	static Type WindowLayoutType
	{
		get
		{
			Type windowLayoutType = typeof (EditorWindow).Assembly.GetType ("UnityEditor.WindowLayout");

			if (object.ReferenceEquals (windowLayoutType, null))
			{
				throw new ApplicationException ("No window layout type");
			}

			return windowLayoutType;
		}
	}


	static PropertyInfo ToolbarLastLoadedLayoutNameProperty
	{
		get
		{
			Type toolbarType = typeof (EditorWindow).Assembly.GetType ("UnityEditor.Toolbar");

			if (object.ReferenceEquals (toolbarType, null))
			{
				throw new ApplicationException ("No toolbar type");
			}

			PropertyInfo lastLoadedLayoutNameProperty = toolbarType.GetProperty ("lastLoadedLayoutName", BindingFlags.Static | BindingFlags.NonPublic);

			if (object.ReferenceEquals (lastLoadedLayoutNameProperty, null))
			{
				throw new ApplicationException ("No last loaded layout property");
			}

			return lastLoadedLayoutNameProperty;
		}
	}


	static string CurrentLayoutName
	{
		get
		{
			return (string)ToolbarLastLoadedLayoutNameProperty.GetValue (null, null);
		}
		set
		{
			MethodInfo setMethod = ToolbarLastLoadedLayoutNameProperty.GetSetMethod (true);

			if (object.ReferenceEquals (setMethod, null))
			{
				throw new ApplicationException ("No set method");
			}

			setMethod.Invoke (null, new object[]{ value });
		}
	}


	static bool GetValidLayoutPath (string name, ref string path)
	{
		string result = "";

		if (!NewAPI)
		{
			result = GetLayoutPath (name);

			if (File.Exists (result))
			{
				path = result;
				return true;
			}

			return false;
		}
		else
		{
			result = GetPreferencesLayoutPath (name);

			if (File.Exists (result))
			{
				path = result;
				return true;
			}

			result = GetProjectLayoutPath (name);

			if (File.Exists (result))
			{
				path = result;
				return true;
			}

			return false;
		}
	}

	
	static string GetLayoutPath (string name)
	{
		MethodInfo getLayoutsPathMethod = WindowLayoutType.GetMethod ("GetLayoutsPath", BindingFlags.Static | BindingFlags.NonPublic);

		if (object.ReferenceEquals (getLayoutsPathMethod, null))
		{
			throw new ApplicationException ("No get layouts path method");
		}

		return Path.Combine ((string)getLayoutsPathMethod.Invoke (null, null), name) + ".wlt";
	}


	static string GetPreferencesLayoutPath (string name)
	{
		return GetLayoutPath (name, "Preferences");
	}


	static string GetProjectLayoutPath (string name)
	{
		return GetLayoutPath (name, "Project");
	}


	static string GetLayoutPath (string name, string type)
	{
		PropertyInfo layoutsProjectPathProperty = WindowLayoutType.GetProperty ("layouts" + type + "Path", BindingFlags.Static | BindingFlags.NonPublic);

		if (object.ReferenceEquals (layoutsProjectPathProperty, null))
		{
			throw new ApplicationException ("No layouts " + type + " path property");
		}

		return Path.Combine ((string)layoutsProjectPathProperty.GetValue (null, null), name) + ".wlt";
	}


	static bool LoadWindowLayout (string path)
	{
		MethodInfo loadWindowLayoutMethod = WindowLayoutType.GetMethod ("LoadWindowLayout", BindingFlags.Static | BindingFlags.Public);

		if (object.ReferenceEquals (loadWindowLayoutMethod, null))
		{
			throw new ApplicationException ("No load window layout method");
		}

		return (bool)loadWindowLayoutMethod.Invoke (null, new object[]{ path });
	}
}
