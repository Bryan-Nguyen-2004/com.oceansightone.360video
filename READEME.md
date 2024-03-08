TO-DO:
ask prof huangs what settings are necessary


send video audio directly to audio listener or send audio to audio source?


how to set default tint and exposure for skybox materials


which objects should i definetely blacklist

make the options for blacklistedTags a dropdown for only the existing tags

To make the blacklistedTags a dropdown of existing tags, you need to create a custom inspector using Unity's Editor scripting. Here's an example of how you can do this:

First, create a new C# script in the Editor folder (create this folder in your Assets directory if it doesn't exist) and name it Video360Editor.cs. Then, paste the following code:

}
This script overrides the default inspector for the Video360 script. It finds the blacklistedTags property and creates a tag field for each element in the array. The EditorGUILayout.TagField function creates a dropdown of all existing tags.

Remember to replace Video360 with the actual name of your script.