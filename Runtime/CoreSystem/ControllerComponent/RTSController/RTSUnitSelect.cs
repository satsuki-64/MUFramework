using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

namespace MUFramework.CoreSystem.RTSController
{
    public enum SelectionMode
    {
        ScreenSpaceAndAllTags,
        BoundBoxWithTags
    }

    /// <summary>
    /// 被选中的物体标签需要为 Unit
    /// </summary>
    public class RTSUnitSelect : MonoBehaviour
    {
        [Header("Selecting Object")]
        public List<GameObject> CurrentUnitSelect = new List<GameObject>();
        
        [Space]
        [Header("Selection Setting")]
        public SelectionMode CurrentSelectMode = SelectionMode.ScreenSpaceAndAllTags;
        public Vector3 BoundBoxGroundPosition = Vector3.zero;
        public float BoundBoxYHeight = 10f;
        [Tooltip("用于区分点击和框选的阈值")]
        public float CLICK_THRESHOLD = 5f;
        
        private Vector3 selectionStartPos;
        private Vector3 selectionCurrentPos;
        private bool isSelecting = false;
        private Camera mainCamera;
        
        [Space]
        [Header("Selection Box Setting")]
        public Color selectionBoxColor = new Color(0.8f, 0.8f, 1.0f, 0.25f);
        public Color selectionBorderColor = new Color(0.8f, 0.8f, 1.0f, 1.0f);
        public float lineWidth = 2.0f;
        [Tooltip("用于绘制框选矩形的填充纹理")]
        public Texture2D fillTexture;
        [Tooltip("用于绘制框选矩形的边缘纹理")]
        public Texture2D borderTexture;
        
        void Start()
        {
            mainCamera = GetComponent<Camera>();
            if (mainCamera == null)
                mainCamera = Camera.main;
                
            CreateTextures();
        }
        
        void Update()
        {
            HandleSelectionInput();
        }

        void OnGUI()
        {
            if (isSelecting && Vector3.Distance(selectionStartPos, selectionCurrentPos) > CLICK_THRESHOLD)
            {
                DrawSelectionBox();
            }
        }
        
        void OnDestroy()
        {
            // 清理纹理
            if (fillTexture != null)
            {
                DestroyImmediate(fillTexture);
            }
            if (borderTexture != null)
            {
                DestroyImmediate(borderTexture);
            }
        }
        
        #region 处理框选输入

        void HandleSelectionInput()
        {
            // 鼠标左键按下
            if (Input.GetMouseButtonDown(0))
            {
                selectionStartPos = Input.mousePosition;
                isSelecting = true;
                
                // 检查是否是点击（非框选）
                RaycastHit hit;
                if (Physics.Raycast(mainCamera.ScreenPointToRay(selectionStartPos), out hit))
                {
                    if (hit.collider.CompareTag("Unit"))
                    {
                        // 点击选中单个单位
                        HandleSingleUnitClick(hit.collider.gameObject);
                        isSelecting = false;
                        return;
                    }
                }
                
                // 如果不是Shift键按下，清空当前选择
                if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                {
                    ClearSelection();
                }
            }
            
            // 鼠标左键抬起
            if (Input.GetMouseButtonUp(0) && isSelecting)
            {
                selectionCurrentPos = Input.mousePosition;
                
                // 检查移动距离是否超过点击阈值
                if (Vector3.Distance(selectionStartPos, selectionCurrentPos) > CLICK_THRESHOLD)
                {
                    PerformBoxSelection();
                }
                
                isSelecting = false;
            }
            
            // 更新当前框选位置
            if (isSelecting)
            {
                selectionCurrentPos = Input.mousePosition;
            }
        }
        
        void HandleSingleUnitClick(GameObject unit)
        {
            ClearSelection();
            AddUnitToSelection(unit);
        }
        
        void ClearSelection()
        {
            // 移除所有单位的选中视觉反馈
            foreach (GameObject unit in CurrentUnitSelect)
            {
                SetUnitSelectionVisual(unit, false);
            }
            CurrentUnitSelect.Clear();
        }
        
        void AddUnitToSelection(GameObject unit)
        {
            // 如果当前选择的 Unit 不为空，并且其标签为 Unit，并且其不在当前 List 当中，则将其加入到 List 当中
            if (unit != null && unit.CompareTag("Unit") && !CurrentUnitSelect.Contains(unit))
            {
                CurrentUnitSelect.Add(unit);
                
                // 这里可以添加单位被选中时的视觉反馈
                SetUnitSelectionVisual(unit, true);
            }
        }

        #endregion

        #region 矩形范围框选处理

        void PerformBoxSelection()
        {
            Rect selectionRect = GetSelectionRect();
            List<GameObject> unitsInRect = new List<GameObject>();

            switch (CurrentSelectMode)
            {
                case SelectionMode.ScreenSpaceAndAllTags:
                    unitsInRect = GetScreenSpaceAndAllTags(selectionRect);
                    break;
                case SelectionMode.BoundBoxWithTags:
                    unitsInRect = GetBoundBoxWithTags(selectionRect);
                    break;
            }
            
            // 如果是Shift键框选，则追加到当前选择
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                foreach (GameObject unit in unitsInRect)
                {
                    AddUnitToSelection(unit);
                }
            }
            else
            {
                // 普通框选，先清空再添加
                ClearSelection();
                foreach (GameObject unit in unitsInRect)
                {
                    AddUnitToSelection(unit);
                }
            }
        }

        Rect GetSelectionRect()
        {
            // 规范化选择区域坐标，确保矩形从起始点扩展到当前点
            Vector2 min = Vector2.Min(selectionStartPos, selectionCurrentPos);
            Vector2 max = Vector2.Max(selectionStartPos, selectionCurrentPos);
            
            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }
        
        List<GameObject> GetScreenSpaceAndAllTags(Rect selectionRect)
        {
            List<GameObject> selectedUnits = new List<GameObject>();
            GameObject[] allUnits = GameObject.FindGameObjectsWithTag("Unit");
            
            foreach (GameObject unit in allUnits)
            {
                if (IsUnitInSelectionRect(unit, selectionRect))
                {
                    selectedUnits.Add(unit);
                }
            }
            
            return selectedUnits;
        }

        bool IsUnitInSelectionRect(GameObject unit, Rect selectionRect)
        {
            // 将单位的世界坐标转换为屏幕坐标
            Vector3 screenPos = mainCamera.WorldToScreenPoint(unit.transform.position);
            
            // 检查单位是否在摄像机视野内，如果不是，则直接排除
            if (screenPos.z < mainCamera.nearClipPlane || screenPos.z > mainCamera.farClipPlane)
                return false;
            
            // 检查当前单位对应的坐标，是否在选择矩形内，如果是则返回真
            return selectionRect.Contains(new Vector2(screenPos.x, screenPos.y));
        }
        
        List<GameObject> GetBoundBoxWithTags(Rect selectionRect)
        {
            List<GameObject> selectedUnits = new List<GameObject>();
            
            // 获取框选区域在世界空间中的包围盒
            Bounds selectionBounds = GetSelectionWorldBounds(selectionRect);
            
            if (selectionBounds.size.magnitude < 0.1f) 
                return selectedUnits;
            
            // 使用 Physics.OverlapBox 检测包围盒内的碰撞体
            Collider[] colliders = Physics.OverlapBox(
                selectionBounds.center, 
                selectionBounds.extents, 
                Quaternion.identity
            );
            
            foreach (Collider collider in colliders)
            {
                GameObject unit = collider.gameObject;
                // 筛选标签为"Unit"的物体
                if (unit.CompareTag("Unit") && !selectedUnits.Contains(unit))
                {
                    selectedUnits.Add(unit);
                }
            }
            
            return selectedUnits;
        }

        Bounds GetSelectionWorldBounds(Rect selectionRect)
        {
            // 从框选矩形的四个角发射射线到场景中
            Vector3[] worldCorners = new Vector3[4];
            
            // 获取四个角的世界坐标
            worldCorners[0] = ScreenToWorldPoint(selectionRect.position); // 左下角
            worldCorners[1] = ScreenToWorldPoint(new Vector2(selectionRect.x, selectionRect.yMax)); // 左上角
            worldCorners[2] = ScreenToWorldPoint(new Vector2(selectionRect.xMax, selectionRect.y)); // 右下角
            worldCorners[3] = ScreenToWorldPoint(new Vector2(selectionRect.xMax, selectionRect.yMax)); // 右上角
            
            // 计算包围盒的中心和大小
            Vector3 min = worldCorners[0];
            Vector3 max = worldCorners[0];
            
            for (int i = 1; i < worldCorners.Length; i++)
            {
                min = Vector3.Min(min, worldCorners[i]);
                max = Vector3.Max(max, worldCorners[i]);
            }
            
            // 考虑单位可能的高度范围，添加一定的垂直容差
            float verticalTolerance = BoundBoxYHeight;
            min.y -= verticalTolerance;
            max.y += verticalTolerance;
            
            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);
            
            return bounds;
        }

        Vector3 ScreenToWorldPoint(Vector2 screenPoint)
        {
            // 将屏幕坐标转换为世界坐标
            Ray ray = mainCamera.ScreenPointToRay(screenPoint);
            
            // 假设单位主要在地面上，使用射线与地面的交点
            // 这里定义一个选择平面，地面位置设置为 BoundBoxGroundPosition
            Plane groundPlane = new Plane(Vector3.up, BoundBoxGroundPosition);
            float enter;
            
            if (groundPlane.Raycast(ray, out enter))
            {
                return ray.GetPoint(enter);
            }
            
            // 如果没有击中地面，返回射线上的一个点（比如距离摄像机一定距离）
            return ray.GetPoint(10f);
        }
        #endregion
        
        #region 可视化相关

        void SetUnitSelectionVisual(GameObject unit, bool isSelected)
        {
            // 这里可以实现单位的选中视觉反馈，例如高亮显示
            // 这里未来将其改为事件队列，可以为当前框选定制显示行为
            Renderer renderer = unit.GetComponent<Renderer>();
            if (renderer != null)
            {
                // 简单的颜色变化示例
                if (isSelected)
                {
                    renderer.material.color = Color.yellow;
                }
                else
                {
                    renderer.material.color = Color.white;
                }
            }
        }
        
        void CreateTextures()
        {
            // 创建填充纹理
            fillTexture = new Texture2D(1, 1);
            fillTexture.SetPixel(0, 0, Color.white);
            fillTexture.Apply();
            
            // 创建边框纹理
            borderTexture = new Texture2D(1, 1);
            borderTexture.SetPixel(0, 0, Color.white);
            borderTexture.Apply();
        }
        
        void DrawSelectionBox()
        {
            float x = selectionStartPos.x;
            float y = Screen.height - selectionStartPos.y; // 转换为GUI坐标系（原点在左上角）
            float width = selectionCurrentPos.x - selectionStartPos.x;
            float height = -(selectionCurrentPos.y - selectionStartPos.y); // 反转Y轴方向
            
            Rect selectionRect = new Rect(x, y, width, height);
            
            // 绘制半透明填充
            GUI.color = selectionBoxColor;
            GUI.DrawTexture(selectionRect, fillTexture);
            
            // 绘制边框
            GUI.color = selectionBorderColor;
            
            // 确保线宽不会因为矩形方向而变负
            float absWidth = Mathf.Abs(width);
            float absHeight = Mathf.Abs(height);
            
            // 根据拖动方向调整边框位置
            if (width >= 0)
            {
                // 从左向右拖动
                if (height >= 0)
                {
                    // 从上向下拖动
                    DrawBorders(selectionRect, absWidth, absHeight);
                }
                else
                {
                    // 从下向上拖动
                    Rect adjustedRect = new Rect(selectionRect.x, selectionRect.y + height, absWidth, absHeight);
                    DrawBorders(adjustedRect, absWidth, absHeight);
                }
            }
            else
            {
                // 从右向左拖动
                if (height >= 0)
                {
                    // 从上向下拖动
                    Rect adjustedRect = new Rect(selectionRect.x + width, selectionRect.y, absWidth, absHeight);
                    DrawBorders(adjustedRect, absWidth, absHeight);
                }
                else
                {
                    // 从下向上拖动
                    Rect adjustedRect = new Rect(selectionRect.x + width, selectionRect.y + height, absWidth, absHeight);
                    DrawBorders(adjustedRect, absWidth, absHeight);
                }
            }
        }
        
        void DrawBorders(Rect rect, float width, float height)
        {
            // 绘制上边框
            Rect topBorder = new Rect(rect.x, rect.y, width, lineWidth);
            GUI.DrawTexture(topBorder, borderTexture);
            
            // 绘制下边框
            Rect bottomBorder = new Rect(rect.x, rect.y + height - lineWidth, width, lineWidth);
            GUI.DrawTexture(bottomBorder, borderTexture);
            
            // 绘制左边框
            Rect leftBorder = new Rect(rect.x, rect.y, lineWidth, height);
            GUI.DrawTexture(leftBorder, borderTexture);
            
            // 绘制右边框
            Rect rightBorder = new Rect(rect.x + width - lineWidth, rect.y, lineWidth, height);
            GUI.DrawTexture(rightBorder, borderTexture);
        }

        #endregion
    }
}