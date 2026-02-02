require('UtilLua')
print("Test")

local obj1 = CS.UnityEngine.GameObject()
local obj2 = CS.UnityEngine.GameObject("新建Object")

GameObject = CS.UnityEngine.GameObject
local obj3 = GameObject("新建Object测试")

local obj4 = GameObject.Find("Main Camera")
print(obj4.transform.position)

Debug = CS.UnityEngine.Debug;
Debug.Log("测试一下在Lua中使用Debug")

Vector3 = CS.UnityEngine.Vector3

function DoCameraMove()
    if IsNull(obj4) == false then
        -- obj4.transform:Translate(Vector3.right)
    else
        print("当前Camera不存在")
    end
end

-- local Dic_String_Vector3 = CS.System.Collections.Generic.Dictionary(CS.System.String,Vector3)
-- local dict1 = Dic_String_Vector3()
-- dict1.Add("123",Vector3.right)
-- print(dic1:get_Item("123"))