using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using System.Text;
using UnityEditor;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.UIElements;

public class MainUI : MonoBehaviour{
    private DataManager dataManager;
    private UIDocument m_Document;
    private UnityEngine.UIElements.Button selectPathBtn, upOneFolder, reimportBtn;
    private Label pathLabel, breadcrumbsLabel, m3NameLbl;
    private ListView itemList;
    private Scroller verticalScroller;
    private VisualElement m3SubmeshOptions;
    private string internalPath = @"AIDX";
    private string filePath = "";
    void Start(){
        this.m_Document = GetComponent<UIDocument>();
        this.selectPathBtn = m_Document.rootVisualElement.Q<UnityEngine.UIElements.Button>("selectPathBtn");
        this.pathLabel = m_Document.rootVisualElement.Q<Label>("pathLabel");
        this.itemList = m_Document.rootVisualElement.Q<ListView>("contentList");
        this.upOneFolder = m_Document.rootVisualElement.Q<UnityEngine.UIElements.Button>("upFolderBtn");
        this.breadcrumbsLabel = m_Document.rootVisualElement.Q<Label>("breadcrumbsLabel");
        this.verticalScroller = this.itemList.Q("unity-content-and-vertical-scroll-container").Q<Scroller>();
        this.m3SubmeshOptions = m_Document.rootVisualElement.Q<VisualElement>("m3SubmeshOptions");
        this.m3NameLbl = m_Document.rootVisualElement.Q<Label>("m3NameLbl");
        this.reimportBtn = m_Document.rootVisualElement.Q<UnityEngine.UIElements.Button>("reimportBtn");
        
        this.selectPathBtn.clicked += () => {
            this.ChooseWSPath();
        };
        this.upOneFolder.clicked += () => {
            this.UpOneFolder();
        };
        this.itemList.onItemsChosen += (item) => {
            this.SelectItem(item);
            // Debug.Log(item.FirstOrDefault());
        };
        this.itemList.RegisterCallback<WheelEvent>(@event => {
            verticalScroller.value += @event.delta.y * 1000;
            // Stop the event here so the builtin scroll functionality of the list doesn't activate
            @event.StopPropagation();
        });
        this.reimportBtn.clicked += () => {
            this.ReimportM3Model();
        };
    }
    void ChooseWSPath(){
		var path = EditorUtility.OpenFilePanel("Wildstar location", this.pathLabel.text, "exe");
        if (path.Length != 0){
			this.pathLabel.text = path;
            dataManager = new DataManager();
            dataManager.InitializeData(path);
            this.ReloadList();
            // var test = @"AIDX\Art\Prop\Natural\Tree\Deciduous_RootyMangrove\PRP_Tree_Deciduous_RootyMangrove_Violet_000.m3";
            // var test = @"AIDX\Art\Creature\Rowsdower\Rowsdower.m3";
            // M3File m3File = new M3File();
            // var m3_header = m3File.Load(test);
            // m3File.ExportM3(test, new int[3]{0,1,2});
            // m3File.RenderM3();
            // this.DisplayM3HeaderDta(m3File);

        }
    }
    void UpOneFolder(){
        if(this.internalPath.Contains("\\")){
            this.internalPath = this.internalPath.Substring(0, this.internalPath.LastIndexOf("\\"));
        }
        this.breadcrumbsLabel.text = this.internalPath;
        this.ReloadList();
    }
    void SelectItem(IEnumerable<object> item){
        var selectedItem = this.itemList.selectedItem as string;
        if(selectedItem.Contains(".m3")){
            this.filePath = this.internalPath + "\\" + selectedItem;
            M3File m3File = new M3File();
            var m3_header = m3File.Load(this.filePath);
            this.DisplayM3HeaderData(m3File);
        }else{
            this.internalPath += "\\" + selectedItem;
            this.breadcrumbsLabel.text = this.internalPath;
            this.ReloadList();
        }
    }
    void CleanDisplay(){
        this.m3SubmeshOptions.Clear();
        foreach(Transform child in GameObject.Find("ActiveObject").transform){
            Destroy(child.gameObject);
        }
    }
    void DisplayM3HeaderData(M3File m3_file){
        this.CleanDisplay();
        var file_name = Path.GetFileNameWithoutExtension(this.filePath);
        this.m3NameLbl.text = file_name;
        for(var i=0;i<m3_file.header.geometry.nrSubmeshes;i++){
            var toggle = new UnityEngine.UIElements.Toggle("Submesh " + i.ToString()){
                value = true
            };
            toggle.style.fontSize = 20;
            toggle.style.color = new Color(1,1,1,1);
            var mesh_id = i;
            var submesh_name = file_name + "-submesh-" + mesh_id.ToString();
            m3_file.DrawSingleSubmesh(i, submesh_name);
            var obj = GameObject.Find(submesh_name);
            obj.transform.parent = GameObject.Find("ActiveObject").transform;
            toggle.RegisterValueChangedCallback((evt) => { 
                obj.SetActive(evt.newValue);
            });
            this.m3SubmeshOptions.Add(toggle);
        }
        
    }
    void ReloadList(){
        var list = dataManager.GetFolderList(this.internalPath);
        List<Label> labelList = new List<Label>();
        foreach(var a_item in list){
            var lbl = new Label();
            lbl.text = a_item;
            lbl.tooltip = a_item;
            labelList.Add(lbl);
        }
        var file_list = dataManager.GetFileList(this.internalPath);
        foreach(var a_file in file_list){
            if(a_file.Key.Contains(".m3")){
                var lbl = new Label();
                lbl.text = a_file.Key;
                lbl.tooltip = a_file.Key;
                labelList.Add(lbl);
                list.Add(a_file.Key);
            }
        }
        this.itemList.itemsSource = list;
        this.itemList.Rebuild();
    }
    void ReimportM3Model(){
        var selectedSubmeshes = new List<int>();
        int count = 0;
        foreach(UnityEngine.UIElements.Toggle a_child in this.m3SubmeshOptions.Children()){
            if(a_child.value){
                selectedSubmeshes.Add(count);
            }
            count++;
        }
        this.CleanDisplay();
        M3File m3File = new M3File();
        m3File.ExportM3(this.filePath, selectedSubmeshes.ToArray());
        var gameObjectName = Path.GetFileNameWithoutExtension(this.filePath);
        var obj = GameObject.Find(gameObjectName).transform;
        obj.transform.parent = GameObject.Find("ActiveObject").transform;
    }
    void Update()
    {
        
    }
}
