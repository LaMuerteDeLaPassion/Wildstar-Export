# Wildstar Export

---
a tool for exporting Wildstar models into unity. The functions are not optimized and are intentionally written to be as clear and straightforward as possible. Therefore, loading models might be a bit slow, depending on the complexity of the model. 

Feel free to submit improvements, especially if it helps to decode more of the models.

### How to use:

1. Open the main scene in Unity. (Made in Unity 2021.3.4f1)
2. Run the scene
3. Click "Select Wildstar Path" and navigate to the Wildstar.exe file. Select that file. 
4. Once the game is loaded, use the navigation menu to find a model you want to view/Export.
5. Click "Export" to export the model. 
---
- Make sure to select only the submeshes you want to export.
- The exported models will be exported as prefabs to your Assets/Resources folder
- Many models do not display correctly. This is usually because the shader is not fully understood.
---
### Still left to implement
- Improve understanding of the shader
- Fix bone rotations/poses for models that have a "left side" (usually creatures, probably has to do with bone mirroring when the models were first created)
- More options for exporting models
---
Archive navigator and extracting raw files is written by Zee 
