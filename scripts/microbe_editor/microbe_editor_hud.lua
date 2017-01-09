-- Updates the hud with relevant information
class 'MicrobeEditorHudSystem' (System)

function MicrobeEditorHudSystem:__init()
    System.__init(self)
    self.organelleButtons = {}
    self.initialized = false
    self.editor = MicrobeEditor(self)
    
    -- Scene nodes for the organelle cursors for symmetry.
    self.hoverHex = {}
    self.hoverOrganelle = {}
    
    self.saveLoadPanel = nil    
    self.creationsListbox = nil
    self.creationFileMap = {} -- Map from player creation name to filepath
    self.activeButton = nil -- stores button, not name
    self.helpPanelOpen = false
    self.menuOpen = false
end


function MicrobeEditorHudSystem:init(gameState)
    System.init(self, "MicrobeEditorHudSystem", gameState)
    self.editor:init(gameState)

    -- This seems really cluttered, there must be a better way.
    for i=1, 42 do
        self.hoverHex[i] = Entity("hover-hex" .. i)
        local sceneNode = OgreSceneNodeComponent()
        sceneNode.transform.position = Vector3(0,0,0)
        sceneNode.transform:touch()
        sceneNode.meshName = "hex.mesh"
        self.hoverHex[i]:addComponent(sceneNode)
    end
    for i=1, 6 do
        self.hoverOrganelle[i] = Entity("hover-organelle" .. i)    
        local sceneNode = OgreSceneNodeComponent()
        sceneNode.transform.position = Vector3(0,0,0)
        sceneNode.transform:touch()
        self.hoverOrganelle[i]:addComponent(sceneNode)
    end
    

    local root = gameState:rootGUIWindow()
    self.mpLabel = root:getChild("MpPanel"):getChild("MpBar"):getChild("NumberLabel")
    self.mpProgressBar = root:getChild("MpPanel"):getChild("MpBar")
    self.mpProgressBar:setProperty("ThriveGeneric/MpBar", "FillImage") 
    
    local nucleusButton = root:getChild("NewButton")
    local flagellumButton = root:getChild("EditPanel"):getChild("StructurePanel"):getChild("AddFlagellum")
    local cytoplasmButton = root:getChild("EditPanel"):getChild("StructurePanel"):getChild("AddCytoplasm")
    local mitochondriaButton = root:getChild("EditPanel"):getChild("StructurePanel"):getChild("AddMitochondrion")
    local vacuoleButton = root:getChild("EditPanel"):getChild("StructurePanel"):getChild("AddVacuole")
    local toxinButton = root:getChild("EditPanel"):getChild("StructurePanel"):getChild("AddToxinVacuole")
    local chloroplastButton = root:getChild("EditPanel"):getChild("StructurePanel"):getChild("AddChloroplast")
    
    self.organelleButtons["nucleus"] = nucleusButton
    self.organelleButtons["flagellum"] = flagellumButton
    self.organelleButtons["cytoplasm"] = cytoplasmButton
    self.organelleButtons["mitochondrion"] = mitochondriaButton
    self.organelleButtons["chloroplast"] = chloroplastButton
    self.organelleButtons["vacuole"] = vacuoleButton
    self.organelleButtons["Toxin"] = toxinButton
    self.activeButton = nil
    
    nucleusButton:registerEventHandler("Clicked", function() self:nucleusClicked() end)
    flagellumButton:registerEventHandler("Clicked", function() self:flagellumClicked() end)
    cytoplasmButton:registerEventHandler("Clicked", function() self:cytoplasmClicked() end)
    mitochondriaButton:registerEventHandler("Clicked", function() self:mitochondriaClicked() end)
    chloroplastButton:registerEventHandler("Clicked", function() self:chloroplastClicked() end)
    vacuoleButton:registerEventHandler("Clicked", function() self:vacuoleClicked() end)
    toxinButton:registerEventHandler("Clicked", function() self:toxinClicked() end)
    
    -- self.saveLoadPanel = root:getChild("SaveLoadPanel")
    -- self.creationsListbox = self.saveLoadPanel:getChild("SavedCreations")
    self.undoButton = root:getChild("UndoButton")
    self.undoButton:registerEventHandler("Clicked", function() self.editor:undo() end)
    self.redoButton = root:getChild("RedoButton")
    self.redoButton:registerEventHandler("Clicked", function() self.editor:redo() end)
    self.symmetryButton = root:getChild("SymmetryButton")
    self.symmetryButton:registerEventHandler("Clicked", function() self:changeSymmetry() end)

    root:getChild("FinishButton"):registerEventHandler("Clicked", playClicked)
    --root:getChild("BottomSection"):getChild("MenuButton"):registerEventHandler("Clicked", self:menuButtonClicked)
    root:getChild("MenuButton"):registerEventHandler("Clicked", function() self:menuButtonClicked() end)
    root:getChild("PauseMenu"):getChild("MainMenuButton"):registerEventHandler("Clicked", function() self:menuMainMenuClicked() end)
    root:getChild("PauseMenu"):getChild("ResumeButton"):registerEventHandler("Clicked", function() self:resumeButtonClicked() end)
    root:getChild("PauseMenu"):getChild("CloseHelpButton"):registerEventHandler("Clicked", function() self:closeHelpButtonClicked() end)
    root:getChild("PauseMenu"):getChild("QuitButton"):registerEventHandler("Clicked", function() self:quitButtonClicked() end)
    --root:getChild("SaveMicrobeButton"):registerEventHandler("Clicked", function() self:saveCreationClicked() end)
    --root:getChild("LoadMicrobeButton"):registerEventHandler("Clicked", function() self:loadCreationClicked() end)

    self.helpPanel = root:getChild("PauseMenu"):getChild("HelpPanel")
    root:getChild("PauseMenu"):getChild("HelpButton"):registerEventHandler("Clicked", function() self:helpButtonClicked() end)
    
    -- Set species name and cut it off if it is too long.
    --[[ local name = self.nameLabel:getText()
    if string.len(name) > 18 then
        name = string.sub(name, 1, 15)
        name = name .. "..."
    end
    self.nameLabel:setText(name) --]]
end


function MicrobeEditorHudSystem:activate()
    global_activeMicrobeEditorHudSystem = self -- Global reference for event handlers
    self.editor:activate()
    for typeName,button in pairs(global_activeMicrobeEditorHudSystem.organelleButtons) do
        print(typeName)
        if Engine:playerData():lockedMap():isLocked(typeName) then
            button:disable()
        else
            button:enable()
        end
    end    
end

function MicrobeEditorHudSystem:setActiveAction(actionName)
    self.editor:setActiveAction(actionName)
    if actionName == "nucleus" then
        -- For now we simply create a new microbe with the nucleus button
        self.editor:performLocationAction()
    end
end


function MicrobeEditorHudSystem:update(renderTime, logicTime)
    for i=1, 42 do
        local sceneNode = self.hoverHex[i]:getComponent(OgreSceneNodeComponent.TYPE_ID)
        sceneNode.transform.position = Vector3(0,0,0)
        sceneNode.transform.scale = Vector3(0,0,0)
        sceneNode.transform:touch()
    end
    for i=1, 6 do
        local sceneNode = self.hoverOrganelle[i]:getComponent(OgreSceneNodeComponent.TYPE_ID)
        sceneNode.transform.position = Vector3(0,0,0)
        sceneNode.transform.scale = Vector3(0,0,0)
        sceneNode.transform:touch()
    end
    self.editor:update(renderTime, logicTime)
	
    -- Handle input
    if Engine.mouse:wasButtonPressed(Mouse.MB_Left) then
        self.editor:performLocationAction()
    end
    if Engine.mouse:wasButtonPressed(Mouse.MB_Right) then
        self:removeClicked()
        self.editor:performLocationAction()
    end	            
    if keyCombo(kmp.togglemenu) then
        self:menuButtonClicked()
    elseif keyCombo(kmp.newmicrobe) then
        -- These global event handlers are defined in microbe_editor_hud.lua
        self:nucleusClicked()
    elseif keyCombo(kmp.redo) then
        self.editor:redo()
    elseif keyCombo(kmp.remove) then
        self:removeClicked()
        self.editor:performLocationAction()
    elseif keyCombo(kmp.undo) then
        self.editor:undo()
    elseif keyCombo(kmp.vacuole) then
        self:vacuoleClicked()
        self.editor:performLocationAction()
    elseif keyCombo(kmp.oxytoxyvacuole) then
        if not Engine:playerData():lockedMap():isLocked("Toxin") then
            self:toxinClicked()
            self.editor:performLocationAction()
        end
    elseif keyCombo(kmp.flagellum) then
        self:flagellumClicked()
        self.editor:performLocationAction()
    elseif keyCombo(kmp.mitochondrion) then
        self:mitochondriaClicked()  
        self.editor:performLocationAction()
    --elseif Engine.keyboard:wasKeyPressed(Keyboard.KC_A) and self.editor.currentMicrobe ~= nil then
    --    self:aminoSynthesizerClicked()
    --    self.editor:performLocationAction()
    elseif keyCombo(kmp.chloroplast) then
        if not Engine:playerData():lockedMap():isLocked("Chloroplast") then
            self:chloroplastClicked()
            self.editor:performLocationAction()
        end
    elseif keyCombo(kmp.togglegrid) then
        if self.editor.gridVisible then
            self.editor.gridSceneNode.visible = false;
            self.editor.gridVisible = false
        else
            self.editor.gridSceneNode.visible = true;
            self.editor.gridVisible = true
        end
    elseif keyCombo(kmp.gotostage) then
        playClicked()
    elseif keyCombo(kmp.rename) then
        self:updateMicrobeName()
    end
    
    if Engine.keyboard:wasKeyPressed(Keyboard.KC_LEFT) or Engine.keyboard:wasKeyPressed(Keyboard.KC_A) then
		self.editor.organelleRot = (self.editor.organelleRot + 60)%360
	end
	if Engine.keyboard:wasKeyPressed(Keyboard.KC_RIGHT) or Engine.keyboard:wasKeyPressed(Keyboard.KC_D) then
		self.editor.organelleRot = (self.editor.organelleRot - 60)%360
	end
	
    if keyCombo(kmp.screenshot) then
        Engine:screenShot("screenshot.png")
    end

    if Engine.keyboard:isKeyDown(Keyboard.KC_LSHIFT) then 
        properties = Entity(CAMERA_NAME .. 3):getComponent(OgreCameraComponent.TYPE_ID).properties
        newFovY = properties.fovY + Degree(Engine.mouse:scrollChange()/10)
        if newFovY < Degree(10) then
            newFovY = Degree(10)
        elseif newFovY > Degree(120) then
            newFovY = Degree(120)
        end
        properties.fovY = newFovY
        properties:touch()
    else
        
    end
end

function MicrobeEditorHudSystem:updateMutationPoints() 
    self.mpProgressBar:progressbarSetProgress(self.editor.mutationPoints/50)
    self.mpLabel:setText("" .. self.editor.mutationPoints)
end

-----------------------------------------------------------------
-- Event handlers -----------------------------------------------


function playClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:setCurrentGameState(GameState.MICROBE)
end

function menuPlayClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:currentGameState():rootGUIWindow():getChild("MenuPanel"):hide()
    playClicked()
end

function MicrobeEditorHudSystem:menuMainMenuClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:setCurrentGameState(GameState.MAIN_MENU)
end

function MicrobeEditorHudSystem:quitButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:quit()
end

-- the rest of the event handlers are MicrobeEditorHudSystem methods

function MicrobeEditorHudSystem:helpButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("HelpPanel"):show()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("CloseHelpButton"):show()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("ResumeButton"):hide()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("QuicksaveButton"):hide()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("SaveGameButton"):hide()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("LoadGameButton"):hide()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("StatsButton"):hide()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("HelpButton"):hide()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("OptionsButton"):hide()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("MainMenuButton"):hide()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("QuitButton"):hide()
    self.helpOpen = not self.helpOpen
end

function MicrobeEditorHudSystem:closeHelpButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("HelpPanel"):hide()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("CloseHelpButton"):hide()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("ResumeButton"):show()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("QuicksaveButton"):show()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("SaveGameButton"):show()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("LoadGameButton"):show()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("StatsButton"):show()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("HelpButton"):show()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("OptionsButton"):show()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("MainMenuButton"):show()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):getChild("QuitButton"):show()
    self.helpOpen = not self.helpOpen
end

function MicrobeEditorHudSystem:nucleusClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self:setActiveAction("nucleus")
end

function MicrobeEditorHudSystem:flagellumClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self.activeButton = self.organelleButtons["flagellum"]
    self.activeButton:disable()
    self:setActiveAction("flagellum")
end

function MicrobeEditorHudSystem:cytoplasmClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self.activeButton = self.organelleButtons["cytoplasm"]
    self.activeButton:disable()
    self:setActiveAction("cytoplasm")
end

function MicrobeEditorHudSystem:mitochondriaClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self.activeButton = self.organelleButtons["mitochondrion"]
    self.activeButton:disable()
    self:setActiveAction("mitochondrion")
end

function MicrobeEditorHudSystem:chloroplastClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self.activeButton = self.organelleButtons["chloroplast"]
    self.activeButton:disable()
    self:setActiveAction("chloroplast")
end

function MicrobeEditorHudSystem:aminoSynthesizerClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self.activeButton = self.organelleButtons["aminosynthesizer"]
    self.activeButton:disable()
    self:setActiveAction("aminosynthesizer")
end

function MicrobeEditorHudSystem:vacuoleClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self.activeButton = self.organelleButtons["vacuole"]
    self.activeButton:disable()
    self:setActiveAction("vacuole")
end

function MicrobeEditorHudSystem:toxinClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self.activeButton = self.organelleButtons["Toxin"]
    self.activeButton:disable()
    self:setActiveAction("oxytoxy")
end


function MicrobeEditorHudSystem:removeClicked()
    if self.activeButton ~= nil then
        self.activeButton:enable()
    end
    self.activeButton = nil
    self:setActiveAction("remove")
end

function MicrobeEditorHudSystem:rootSaveCreationClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    print ("Save button clicked")
    --[[
    panel = self.saveLoadPanel
    panel:getChild("SaveButton"):show()
    panel:getChild("NameTextbox"):show()
    panel:getChild("CreationNameDialogLabel"):show()
    panel:getChild("LoadButton"):hide()
    panel:getChild("SavedCreations"):hide()
    panel:show()--]]
end
--[[
function MicrobeEditorHudSystem:rootLoadCreationClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    panel = self.saveLoadPanel
    panel:getChild("SaveButton"):hide()
    panel:getChild("NameTextbox"):hide()
    panel:getChild("CreationNameDialogLabel"):hide()
    panel:getChild("LoadButton"):show()
    panel:getChild("SavedCreations"):show()
    panel:show()
    self.creationsListbox:itemListboxResetList()
    self.creationFileMap = {}
    i = 0
    pathsString = Engine:getCreationFileList("microbe")
    -- using pattern matching for splitting on spaces
    for path in string.gmatch(pathsString, "%S+")  do
        -- this is unsafe when one of the paths is, for example, C:\\Application Data\Thrive\saves
        item = CEGUIWindow("Thrive/ListboxItem", "creationItems"..i)
        pathSep = package.config:sub(1,1) -- / for unix, \ for windows
        text = string.sub(path, string.len(path) - string.find(path:reverse(), pathSep) + 2)
        item:setText(text)
        self.creationsListbox:itemListboxAddItem(item)
        self.creationFileMap[text] = path
        i = i + 1
    end
    self.creationsListbox:itemListboxHandleUpdatedItemData()
end

function MicrobeEditorHudSystem:saveCreationClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    name = self.editor.currentMicrobe.microbe.speciesName
    print("saving "..name)
    -- Todo: Additional input sanitation
    name, _ = string.gsub(name, "%s+", "_") -- replace whitespace with underscore
    if string.match(name, "^[%w_]+$") == nil then
        print("unsanitary name: "..name) -- should we do the test before whitespace sanitization?
    elseif string.len(name) > 0 then
        Engine:saveCreation(self.editor.currentMicrobe.entity.id, name, "microbe")
    end
end

function MicrobeEditorHudSystem:loadCreationClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    item = self.creationsListbox:itemListboxGetLastSelectedItem()
    if not item:isNull() then 
        entity = Engine:loadCreation(self.creationFileMap[item:getText()])
        self.editor:loadMicrobe(entity)
        panel:hide()
    end
end
--]]

-- useful debug functions

function MicrobeEditorHudSystem:loadByName(name)
    if string.find(name, ".microbe") then 
        print("note, you don't need to add the .microbe extension") 
    else 
        name = name..".microbe"
    end
    name, _ = string.gsub(name, "%s+", "_")
    creationFileMap = {}
    i = 0
    pathsString = Engine:getCreationFileList("microbe")
    -- using pattern matching for splitting on spaces
    for path in string.gmatch(pathsString, "%S+")  do
        -- this is unsafe when one of the paths is, for example, C:\\Application Data\Thrive\saves
        pathSep = package.config:sub(1,1) -- / for unix, \ for windows
        text = string.sub(path, string.len(path) - string.find(path:reverse(), pathSep) + 2)
        creationFileMap[text] = path
        i = i + 1
    end
    entity = Engine:loadCreation(creationFileMap[name])
    self.editor:loadMicrobe(entity)
    --self.nameLabel:setText(self.editor.currentMicrobe.microbe.speciesName)
end

function MicrobeEditorHudSystem:changeSymmetry()
    self.editor.symmetry = (self.editor.symmetry+1)%4
    
    if self.editor.symmetry == 0 then
        self.symmetryButton:getChild("2xSymmetry"):hide()
        self.symmetryButton:getChild("4xSymmetry"):hide()
        self.symmetryButton:getChild("6xSymmetry"):hide()
    elseif self.editor.symmetry == 1 then
        self.symmetryButton:getChild("2xSymmetry"):show()
        self.symmetryButton:getChild("4xSymmetry"):hide()
        self.symmetryButton:getChild("6xSymmetry"):hide()
    elseif self.editor.symmetry == 2 then
        self.symmetryButton:getChild("2xSymmetry"):hide()
        self.symmetryButton:getChild("4xSymmetry"):show()
        self.symmetryButton:getChild("6xSymmetry"):hide()
    elseif self.editor.symmetry == 3 then
        self.symmetryButton:getChild("2xSymmetry"):hide()
        self.symmetryButton:getChild("4xSymmetry"):hide()
        self.symmetryButton:getChild("6xSymmetry"):show()
    end
end

function saveMicrobe() global_activeMicrobeEditorHudSystem:saveCreationClicked() end
function loadMicrobe(name) global_activeMicrobeEditorHudSystem:loadByName(name) end

function MicrobeEditorHudSystem:menuButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    print("played sound")
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):show()
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):moveToFront()
    Engine:pauseGame()
    self.menuOpen = true
end

function MicrobeEditorHudSystem:resumeButtonClicked()
    local guiSoundEntity = Entity("gui_sounds")
    guiSoundEntity:getComponent(SoundSourceComponent.TYPE_ID):playSound("button-hover-click")
    print("played sound")
    Engine:currentGameState():rootGUIWindow():getChild("PauseMenu"):hide()
    Engine:resumeGame()
    self.menuOpen = false
end
