-- 常用别名
require('Object')
require('SplitTools')
-- Lua中JSON解析
JSON = require('JsonUtility')

-- Unity相关
GameObject = CS.UnityEngine.GameObject
Resources = CS.UnityEngine.Resources
Transform = CS.UnityEngine.Transform
RectTransform = CS.UnityEngine.RectTransform
SpriteAtlas = CS.UnityEngine.U2D.SpriteAtlas
Vector3 = CS.UnityEngine.Vector3
Vector2 = CS.UnityEngine.Vector2
TextAsset = CS.UnityEngine.TextAsset

-- UI 相关  
UI = CS.UnityEngine.UI 
Image = UI.Image
Text = UI.Text 
Button = UI.Button
Toggle = UI.Toggle
ScrollRect = UI.ScrollRect
-- Canvas = GameObject.Find("Canvas").transform
UIBehaviour = CS.UnityEngine.EventSystems.UIBehaviour

-- 自己写的C#脚本相关
ABMgr = CS.LuaManager.GetInstance()