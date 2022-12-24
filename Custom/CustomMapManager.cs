using Satchel.Futils;
using TMPro;

namespace Satchel
{
    /// <summary>
    /// defines a custom mapZone
    /// </summary>
    public class customMapZone{
        public string ZoneName;
        public string mapZoneTitle;

        public List<string> sceneNames = new List<string>();
        public Dictionary<string,customMap> scenes = new Dictionary<string,customMap>();
        public Func<bool> hasMap;
        public Func<bool> hasFullMap;
        public GameObject areaCustomMap;
        public Vector2 position;
        public Vector2 titleOffset;
    }
    /// <summary>
    /// defines a single custom map n a zone
    /// </summary>
    public class customMap{
        public string ZoneName;
        public string sceneName;
        public string sceneTitle;
        public Func<bool> hasMap;
        //public GameObject areaCustomMap;
        public Vector2 position;
        public Vector2 titleOffset;
        public Func<Sprite> GetSprite;
    }

    /// <summary>
    /// Handles custom maps
    /// </summary>
    public class CustomMapManager
    {
        private Dictionary<string,customMap> Maps = new Dictionary<string,customMap>();
        private Dictionary<string,customMapZone> Zones = new Dictionary<string,customMapZone>();

        private List<string> MapAreas = new List<string>{"mapCrossroads", "mapGreenpath", "mapFogCanyon", "mapRoyalGardens", "mapFungalWastes", "mapCity", "mapWaterways", "mapMines", "mapDeepnest", "mapCliffs", "mapOutskirts", "mapRestingGrounds", "mapAbyss"};

        private GameMap gameMapComponent;

        private GameObject AreaNameTxt;

        public CustomMapManager(){
                On.GameManager.SetGameMap += OnGameManagerSetGameMap;
                On.GameMap.WorldMap += NewWorldMap;
                On.GameMap.CloseQuickMap += NewCloseQuickMap;
                On.GameMap.PositionCompass += NewPositionCompass;
                On.HutongGames.PlayMaker.Actions.PlayerDataBoolTest.OnEnter += HandlePlayerDataBoolTest;
                ModHooks.LanguageGetHook += LanguageGet;
        }
        /// <summary>
        /// Add a new customMapZone
        /// </summary>
        /// <param name="newZone"></param>
        public void AddZone(customMapZone newZone){
            Zones.Add(newZone.ZoneName,newZone);
        }
        /// <summary>
        /// Add a new customMap scene
        /// </summary>
        /// <param name="map"></param>
        public void AddScene( customMap map){
            Maps.Add(map.sceneName,map);
            if(Zones.TryGetValue(map.ZoneName, out var zone)){
                // if zone exists add scene to zone
                if(!zone.sceneNames.Contains(map.sceneName)){
                    zone.sceneNames.Add(map.sceneName);
                }
                zone.scenes[map.sceneName] = map;
            } else {
                // if zone does not exist create a zone with the scene map's parameters
                zone = new customMapZone(){
                    ZoneName=map.ZoneName,
                    mapZoneTitle=map.ZoneName,
                    sceneNames= new List<string>(){map.sceneName},
                    scenes= new Dictionary<string,customMap>(),
                    hasMap= map.hasMap,
                    position= new Vector2(map.position.x,map.position.y),
                    titleOffset=map.titleOffset,
                };
                map.position = new Vector2(0,0);
                zone.scenes.Add(map.sceneName,map);
                Zones.Add(zone.ZoneName,zone);
            }
            CoroutineHelper.GetRunner().StartCoroutine(generateCustomMap(map));
        }


        public void Remove(string sceneName){
            //Maps.TryGetValue(sceneName,out var oldCustomMap);
            //GameObject.Destroy(oldCustomMap.areaCustomMap);
            //Maps.Remove(sceneName);
        }

        private GameObject GetAreaNameTxt(){
            if(AreaNameTxt== null){
                var quickMapGameObject = GameObject.Find("Quick Map");
                var quickMapFsm = quickMapGameObject.LocateMyFSM("Quick Map");
                var quickMapFsmVars = quickMapFsm.FsmVariables;
                AreaNameTxt = quickMapFsm.GetAction<SetTextMeshProText>("Cliffs", 2).gameObject.GameObject.Value;
            }
            return AreaNameTxt;
        }

        private void AddSceneToZone(customMapZone Zone,customMap map){
            Satchel.Instance.Log($"processing scene {map.sceneName} for map");
            var newZone = Zones[map.ZoneName];
            var tmpChildZ = gameMapComponent.areaCliffs.transform.GetChild(6).localPosition.z;
            var roomMat = UnityEngine.Object.Instantiate(gameMapComponent.areaCliffs.transform.GetChild(1).GetComponent<SpriteRenderer>().material);

            var mapSceneObj = new GameObject(map.sceneName);
            mapSceneObj.transform.SetParent(newZone.areaCustomMap.transform);
            mapSceneObj.name = map.sceneName;
            mapSceneObj.transform.localPosition = new Vector3(map.position.x,map.position.y,tmpChildZ);
            mapSceneObj.layer = 5;
            mapSceneObj.transform.localScale = Vector3.one;

            var sr = mapSceneObj.AddComponent<SpriteRenderer>();
            sr.material = roomMat;
            sr.sprite = map.GetSprite();
            sr.sortingLayerID = 629535577;
            sr.sortingOrder = 0;
            mapSceneObj.SetActive(true);
        }
        private IEnumerator generateCustomMap(customMap map){
            yield return new WaitWhile(()=> gameMapComponent == null);
            var newZone = Zones[map.ZoneName];
            if(newZone.areaCustomMap == null){
                //Need to create Mapzone
                var areaNamePrefab = GameObject.Instantiate(gameMapComponent.areaCliffs.transform.GetChild(0).gameObject);
                areaNamePrefab.SetActive(false);
                newZone.areaCustomMap =  GameObject.Instantiate(gameMapComponent.areaCliffs, gameMapComponent.transform);
                newZone.areaCustomMap.SetActive(true);
                for (int i = 0; i < newZone.areaCustomMap.transform.childCount; i++)
                {
                    GameObject.Destroy(newZone.areaCustomMap.transform.GetChild(i).gameObject);
                }
                newZone.areaCustomMap.name = "dc_custom_mapzone_" + newZone.ZoneName;
                newZone.areaCustomMap.layer = 5;
                newZone.areaCustomMap.transform.localScale = Vector3.one;
                newZone.areaCustomMap.transform.localPosition = new Vector3(newZone.position.x, newZone.position.y , gameMapComponent.areaCliffs.transform.localPosition.z);
            
                var zoneCustomName = GameObject.Instantiate(areaNamePrefab, newZone.areaCustomMap.transform);
                zoneCustomName.transform.position = new Vector3(0,0,0);
                zoneCustomName.transform.localPosition = new Vector3(5f + newZone.titleOffset.x, -0.5f + newZone.titleOffset.y, zoneCustomName.transform.localPosition.z - 0.002f);
                zoneCustomName.GetComponent<SetTextMeshProGameText>().convName = "dc_custom_mapzone_" + newZone.ZoneName;
                zoneCustomName.SetActive(true);
                newZone.areaCustomMap.SetActive(false);
                GameObject.DontDestroyOnLoad(newZone.areaCustomMap);
            }
            // Map zone will always exist now, so add your scene map
            AddSceneToZone(newZone,map);
        }
        
        protected void EditQuickMapFsm(GameObject gameMapGameObject)
        {
            
            var quickMapGameObject = GameObject.Find("Quick Map");
            var quickMapFsm = quickMapGameObject.LocateMyFSM("Quick Map");

            var quickMapFsmVars = quickMapFsm.FsmVariables;

            var customFsmAction = new CustomFsmAction(){
                method = ()=>CheckOpenCustomQuickMap(gameMapGameObject)
            };

            quickMapFsm.InsertAction("Crossroads", customFsmAction, 7);
            quickMapFsm.InsertAction("Dirtmouth", customFsmAction, 5);
            quickMapFsm.InsertAction("Greenpath", customFsmAction, 6);
            quickMapFsm.InsertAction("Cliffs", customFsmAction, 6);
            quickMapFsm.InsertAction("Fungal Wastes", customFsmAction, 6);
            quickMapFsm.InsertAction("City", customFsmAction, 6);
            quickMapFsm.InsertAction("Mines", customFsmAction, 6);
            quickMapFsm.InsertAction("Resting Grounds", customFsmAction, 6);
            quickMapFsm.InsertAction("Fog Canyon", customFsmAction, 6);
            quickMapFsm.InsertAction("Royal Gardens", customFsmAction, 6);
            quickMapFsm.InsertAction("Deepnest", customFsmAction, 6);
            quickMapFsm.InsertAction("Waterways", customFsmAction, 6);
            quickMapFsm.InsertAction("Abyss", customFsmAction, 6);
            quickMapFsm.InsertAction("Outskirts", customFsmAction, 7);


        }
        private void OpenCustomMap(GameMap gm){
            var currentSceneName = SceneUtils.getCurrentScene().name;
            if(Maps.TryGetValue(currentSceneName,out var currentScene)){
                gm.areaGreenpath.SetActive(false);
                gm.areaAncientBasin.SetActive(false);
                gm.areaCity.SetActive(false);
                gm.areaCliffs.SetActive(false);
                gm.areaCrossroads.SetActive(false);
                gm.areaCrystalPeak.SetActive(false);
                gm.areaDeepnest.SetActive(false);
                gm.areaFogCanyon.SetActive(false);
                gm.areaFungalWastes.SetActive(false);
                gm.areaGreenpath.SetActive(false);
                gm.areaKingdomsEdge.SetActive(false);
                gm.areaQueensGardens.SetActive(false);
                gm.areaRestingGrounds.SetActive(false);
                gm.areaDirtmouth.SetActive(false);
                gm.areaWaterways.SetActive(false);

                resetMapVisibility();
                foreach(var kvp in Zones){
                    if(kvp.Value.sceneNames.Contains(currentSceneName)){
                        kvp.Value.areaCustomMap.SetActive(true);
                    }
                }
                var areaTxt = GetAreaNameTxt();
                areaTxt.GetComponent<TextMeshPro>().text = Zones[currentScene.ZoneName].mapZoneTitle != "" ? Zones[currentScene.ZoneName].mapZoneTitle : Zones[currentScene.ZoneName].ZoneName;
                MoveMapTo(gm,
                new Vector2(
                    (Zones[currentScene.ZoneName].position.x*(-1.5365f) +currentScene.position.x),
                    (Zones[currentScene.ZoneName].position.y*(-1.5365f) +currentScene.position.y)
                     ));
            }
        }
        private void MoveMapTo(GameMap gm, Vector2 point){
            gm.gameObject.transform.localPosition = new Vector3(point.x,point.y,18);
        }
        private void CheckOpenCustomQuickMap(GameObject go)
        {
            OpenCustomMap(go.GetComponent<GameMap>());   
        }

        private string LanguageGet( string key, string sheet, string orig){
            //string orig = Language.Language.GetInternal(key, sheet);

            if(key.StartsWith("dc_custom_map_")){
                var sceneName = key.Replace("dc_custom_map_","");
                if(Maps.TryGetValue(sceneName, out var map)){
                    return map.sceneTitle != "" ? map.sceneTitle : map.sceneName;
                }
            }
            if(key.StartsWith("dc_custom_mapzone_")){
                var ZoneName = key.Replace("dc_custom_mapzone_","");
                if(Zones.TryGetValue(ZoneName, out var zone)){
                    return zone.mapZoneTitle != "" ? zone.mapZoneTitle : zone.ZoneName;
                }
            }
            return orig;
        }
        private void HandlePlayerDataBoolTest(On.HutongGames.PlayMaker.Actions.PlayerDataBoolTest.orig_OnEnter orig, HutongGames.PlayMaker.Actions.PlayerDataBoolTest self){
            if(MapAreas.Contains(self.boolName.Value) && Maps.TryGetValue(SceneUtils.getCurrentScene().name,out var currentMap)){
                if(currentMap.hasMap() || Zones[currentMap.ZoneName].hasFullMap())
                {
                    ((FsmStateAction)self).Fsm.Event(self.isTrue);
                } else {
                    ((FsmStateAction)self).Fsm.Event(self.isFalse);
                }
            } else {
                orig(self);
            }
        }
        private void OnGameManagerSetGameMap(On.GameManager.orig_SetGameMap orig, GameManager self, GameObject gameMapGo)
        {   
            //update FSM here
            GameObject.DontDestroyOnLoad(gameMapGo);
            gameMapComponent = gameMapGo.GetComponent<GameMap>();
            EditQuickMapFsm(gameMapGo); 
            orig(self, gameMapGo);
        }

        protected void NewWorldMap(On.GameMap.orig_WorldMap orig, GameMap self)
    {
        orig(self);

        self.panMinX = float.MinValue;
        self.panMaxX = float.MaxValue;
        self.panMinY = float.MinValue;
        self.panMaxY = float.MaxValue;

        var pd = PlayerData.instance;
        if (pd.GetBool("equippedCharm_2")){
            foreach(var kvp in Zones){
                kvp.Value.areaCustomMap.SetActive(kvp.Value.hasMap());
            }
        }
              
    }

        private void resetMapVisibility(){
            foreach(var kvp in Zones){
                foreach(var scene in kvp.Value.sceneNames){
                   var sceneObj = kvp.Value.areaCustomMap.FindGameObjectInChildren(scene);
                   sceneObj.SetActive(kvp.Value.scenes[scene].hasMap() || kvp.Value.hasFullMap());
                }
                kvp.Value.areaCustomMap.SetActive(false);
            }
        }
        protected void NewCloseQuickMap(On.GameMap.orig_CloseQuickMap orig, GameMap self)
        {
            orig(self);
            resetMapVisibility();
        }

        protected void NewPositionCompass(On.GameMap.orig_PositionCompass orig, GameMap self, bool posShade)
        {
            GameObject gameObject = null;
            var currentMapZone = ReflectionHelper.GetField<GameMap, GameManager>(self, "gm").GetCurrentMapZone();

            string sceneName;
            if (!self.inRoom)
            {
                sceneName = ReflectionHelper.GetField<GameMap, GameManager>(self, "gm").sceneName;
            }
            else
            {
                currentMapZone = self.doorMapZone;
                sceneName = self.doorScene;
            }

            if (currentMapZone == "ABYSS")
            {
                gameObject = self.areaAncientBasin;
                self.currentScene = gameObject.FindGameObjectInChildren(sceneName);
            }
            else if (currentMapZone == "CITY" || currentMapZone == "KINGS_STATION" || currentMapZone == "SOUL_SOCIETY" || currentMapZone == "LURIENS_TOWER")
            {
                gameObject = self.areaCity;
                self.currentScene = gameObject.FindGameObjectInChildren(sceneName);
            }
            else if (currentMapZone == "CLIFFS")
            {
                gameObject = self.areaCliffs;
                self.currentScene = gameObject.FindGameObjectInChildren(sceneName);
            }
            else if (currentMapZone == "CROSSROADS" || currentMapZone == "SHAMAN_TEMPLE")
            {
                gameObject = self.areaCrossroads;
                self.currentScene = gameObject.FindGameObjectInChildren(sceneName);
            }
            else if (currentMapZone == "MINES")
            {
                gameObject = self.areaCrystalPeak;
                self.currentScene = gameObject.FindGameObjectInChildren(sceneName);
            }
            else if (currentMapZone == "DEEPNEST" || currentMapZone == "BEASTS_DEN")
            {
                gameObject = self.areaDeepnest;
                self.currentScene = gameObject.FindGameObjectInChildren(sceneName);
            }
            else if (currentMapZone == "FOG_CANYON" || currentMapZone == "MONOMON_ARCHIVE")
            {
                gameObject = self.areaFogCanyon;
                self.currentScene = gameObject.FindGameObjectInChildren(sceneName);
            }
            else if (currentMapZone == "WASTES" || currentMapZone == "QUEENS_STATION")
            {
                gameObject = self.areaFungalWastes;
                self.currentScene = gameObject.FindGameObjectInChildren(sceneName);
            }
            else if (currentMapZone == "GREEN_PATH")
            {
                gameObject = self.areaGreenpath;
                self.currentScene = gameObject.FindGameObjectInChildren(sceneName);
            }
            else if (currentMapZone == "OUTSKIRTS" || currentMapZone == "HIVE" || currentMapZone == "COLOSSEUM")
            {
                gameObject = self.areaKingdomsEdge;
                self.currentScene = gameObject.FindGameObjectInChildren(sceneName);
            }
            else if (currentMapZone == "ROYAL_GARDENS")
            {
                gameObject = self.areaQueensGardens;
                self.currentScene = gameObject.FindGameObjectInChildren(sceneName);
            }
            else if (currentMapZone == "RESTING_GROUNDS")
            {
                gameObject = self.areaRestingGrounds;
                self.currentScene = gameObject.FindGameObjectInChildren(sceneName);
            }
            else if (currentMapZone == "TOWN" || currentMapZone == "KINGS_PASS")
            {
                gameObject = self.areaDirtmouth;
                self.currentScene = gameObject.FindGameObjectInChildren(sceneName);
            }
            else if (currentMapZone == "WATERWAYS" || currentMapZone == "GODSEEKER_WASTE")
            {
                gameObject = self.areaWaterways;
                self.currentScene = gameObject.FindGameObjectInChildren(sceneName);
            }

            self.gameObject.transform.localPosition = new Vector3(0, 0, 0); // test

            if (Maps.TryGetValue(sceneName, out var map))
            {
                //this is custom scene
                gameObject = Zones[map.ZoneName].areaCustomMap;
                self.currentScene = gameObject.FindGameObjectInChildren(sceneName);
            }

            if (self.currentScene != null)
            {
                self.currentScenePos =
                    new Vector3(self.currentScene.transform.localPosition.x + gameObject.transform.localPosition.x,
                        self.currentScene.transform.localPosition.y + gameObject.transform.localPosition.y, 0f);

                if (!posShade && !ReflectionHelper.GetField<GameMap, bool>(self, "posGate"))
                {
                    if (ReflectionHelper.GetField<GameMap, PlayerData>(self, "pd").GetBool("equippedCharm_2"))
                    {
                        ReflectionHelper.SetField(self, "displayingCompass", true);
                        self.compassIcon.SetActive(true);
                    }
                    else
                    {
                        ReflectionHelper.SetField(self, "displayingCompass", false);
                        self.compassIcon.SetActive(false);
                    }
                }

                if (posShade)
                {
                    if (!self.inRoom)
                    {
                        self.shadeMarker.transform.localPosition =
                            new Vector3(self.currentScenePos.x, self.currentScenePos.y, 0f);
                    }
                    else
                    {
                        var x = self.currentScenePos.x -
                                self.currentScene.GetComponent<SpriteRenderer>().sprite.rect.size.x / 100f / 2f +
                                (self.doorX + self.doorOriginOffsetX) / self.doorSceneWidth *
                                (self.currentScene.GetComponent<SpriteRenderer>().sprite.rect.size.x / 100f *
                                 self.transform.localScale.x) / self.transform.localScale.x;
                        var y = self.currentScenePos.y -
                                self.currentScene.GetComponent<SpriteRenderer>().sprite.rect.size.y / 100f / 2f +
                                (self.doorY + self.doorOriginOffsetY) / self.doorSceneHeight *
                                (self.currentScene.GetComponent<SpriteRenderer>().sprite.rect.size.y / 100f *
                                 self.transform.localScale.y) / self.transform.localScale.y;
                        self.shadeMarker.transform.localPosition = new Vector3(x, y, 0f);
                    }

                    ReflectionHelper.GetField<GameMap, PlayerData>(self, "pd").SetVector3("shadeMapPos",
                        new Vector3(self.currentScenePos.x, self.currentScenePos.y, 0f));
                }

                if (ReflectionHelper.GetField<GameMap, bool>(self, "posGate"))
                {
                    self.dreamGateMarker.transform.localPosition =
                        new Vector3(self.currentScenePos.x, self.currentScenePos.y, 0f);
                    ReflectionHelper.GetField<GameMap, PlayerData>(self, "pd").SetVector3("dreamgateMapPos",
                        new Vector3(self.currentScenePos.x, self.currentScenePos.y, 0f));
                }
            }
            else
            {
                Satchel.Instance.Log("Couldn't find current scene object!");
                if (posShade)
                {
                    ReflectionHelper.GetField<GameMap, PlayerData>(self, "pd")
                        .SetVector3("shadeMapPos", new Vector3(-10000f, -10000f, 0f));
                    self.shadeMarker.transform.localPosition = ReflectionHelper.GetField<GameMap, PlayerData>(self, "pd")
                        .GetVector3("shadeMapPos");
                }
            }
        }
    
    }
}