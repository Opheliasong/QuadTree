using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using VGameData;
using UnityStandardAssets.ImageEffects;
//using CodeStage.AntiCheat.Detectors;
//using CodeStage.AntiCheat.ObscuredTypes;

public struct pathNode
{
	//public Transform _trans;
	//public Transform _parent;

	public IntVector2 _index;
	public IntVector2 _parentIndex;
	public float _distance;
}

public class GameManager : MonoBehaviour {

	private static GameManager s_instance = null;


    //GameObject m_console;
    //GameObject set_quit;
	Transform mark_loading;
	public static GameManager Instance
	{
		get
		{
			if (null == s_instance)
			{
				s_instance = FindObjectOfType(typeof(GameManager)) as GameManager;
				if (null == s_instance)
				{
					//Debug.Log("Fail to get GameManager Instance");
					GameObject container = new GameObject("GameManager");
					s_instance = container.AddComponent(typeof(GameManager)) as GameManager;

					Transform bg_loading = (Transform)Instantiate((Resources.Load("bg_loading") as GameObject).transform);
					bg_loading.gameObject.SetActive(false);
					bg_loading.parent = container.transform;
				}
			}
			return s_instance;
		}
	}

	bool netWorkIng = false;
	bool netWorkIngToggle = false;

	public int screenWidth = 1280;
	public int screenHeight = 800;

	UICamera camUI;

	void Awake()
	{
		DontDestroyOnLoad(this);
		Localization.language = "Korean";
		float ratio_real = (float)Screen.width/(float)Screen.height;
		float ratio_ideal = (float)SCREEN.WIDTH/(float)SCREEN.HEIGHT;
		
		if (ratio_real > ratio_ideal)
			screenWidth = (int)(screenHeight * ratio_real);
		else
			screenHeight = (int)(screenWidth / ratio_real);

		//VUserManager.Instance.CheckDataVersion();	// 데이터 체크.
#if UNITY_EDITOR
		VUserManager.Instance.OnMoveScene(SceneManager.GetActiveScene().name);
		if (VUserManager.Instance.IsIntro() == false)
		{
			if (VUserManager.Instance.IsValidData() == false)
			{
				VUserManager.Instance.LoadData();
				VPlayerManager.Instance.LoadData();
                VMUnitElement.Instance.LoadData();
				VItemManager.Instance.LoadAllData();
				VEquipManager.Instance.LoadAllData();
				VMPartySkill.Instance.LoadData();
                VFieldManager.Instance.LoadData();
                VGDungeonManager.Instance.LoadData();
            }
		}
#endif
	}

	const int MAP_XY = 60;
	Transform[,] tiles = new Transform[MAP_XY,MAP_XY];
	List <IntVector2> validTiles = new List<IntVector2>();
	List <IntVector2> breakTiles = new List<IntVector2>();

	public void SceneStart_Camp()
	{
		Debug.Log("[GameManager]: Camp Start");
	}
	public void SceneStart_Stage()
	{
		Debug.Log("[GameManager]: Stage Start");

		if (loadSceneOn == false)
		{
			Transform bg_loading = transform.GetChild(0);
			bg_loading.gameObject.SetActive(true);
			bg_loading.GetComponent<MeshRenderer>().material.SetColor("_TintColor",Color.black);
		}
	}

	MeshRenderer gauge_loading;
	Transform set_loading;
	public void SetLoadingGauge(float _value, string _sceneName = "")
	{
		if (set_loading == null)
		{
			set_loading = ResourcesLoadGameObject("ui/set_loading");
			set_loading.parent = transform;
			gauge_loading = set_loading.GetChild(0).GetComponent<MeshRenderer>();
		}
		switch(_sceneName)
		{
		case "Camp":
			set_loading.GetChild(1).GetComponent<TextMesh>().text = Localization.Get ("msg_campLoading");
			break;
		case "Stage":
			set_loading.GetChild(1).GetComponent<TextMesh>().text = Localization.Get ("msg_nextStageLoading");
			break;
		default:
			break;
		}
		
		if (_value >= 1)
		{
			set_loading.gameObject.SetActive(false);
		}
		else
		{
			set_loading.gameObject.SetActive(true);
			gauge_loading.material.mainTextureOffset = Vector2.right * _value * 0.5f;
		}
	}

	public UICamera GetCamUI()
	{
		if (camUI == null)
			camUI = GameObject.FindWithTag("uiRoot").transform.GetChild(0).GetComponent<UICamera>();
		return camUI;
	}

	public void AddUnitCullingMask(bool _on)
	{
		if (_on)
		{
			LayerMask layerMask = LayerMask.GetMask(new string[2]{"gui","unit"});
			GetCamUI().GetComponent<Camera>().cullingMask = layerMask;
		}
		else
		{
			LayerMask layerMask = LayerMask.GetMask(new string[1]{"gui"});	
			GetCamUI().GetComponent<Camera>().cullingMask = layerMask;
		}
	}

	GameObject uiRoot = null;
	public GameObject GetRootUI()
	{
		if (uiRoot == null)
		{
			uiRoot = GameObject.FindWithTag("uiRoot");
		}
		return uiRoot;
	}

	Transform exitTileTrans = null;
	public Transform GetExitTileTrans()
	{
		return exitTileTrans;
	}

	public void BlurOn(bool _on)
	{
		if(_on)
			StartCoroutine(FadeInBlurInvoke());
		else
			StartCoroutine(FadeOutBlurInvoke());
	}

	IEnumerator FadeInBlurInvoke()
	{
		BlurSimple blur = Camera.main.GetComponent<BlurSimple>();
		blur.enabled = true;
		float blurSize = 0;
		while(blurSize<3)
		{
			blurSize = Mathf.MoveTowards(blurSize, 3, Time.deltaTime *8);
			blur.blurSize = blurSize;
			yield return null;
		}
	}
	IEnumerator FadeOutBlurInvoke()
	{
		BlurSimple blur = Camera.main.GetComponent<BlurSimple>();
		float blurSize = blur.blurSize;
		while(blurSize>0)
		{
			blurSize = Mathf.MoveTowards(blurSize, 0, Time.deltaTime *8);
			blur.blurSize = blurSize;
			yield return null;
		}
		blur.enabled = false;
	}

	/*public void ClearCollider()
	{
		for (int x = 0; x<MAP_XY; ++x)
		{
			for (int y = 0; y <MAP_XY; ++y)
			{
				if (tiles[x,y] ==null)
					continue;
				if (tiles[x,y].GetComponent<BoxCollider>() != null)
					Destroy(tiles[x,y].GetComponent<BoxCollider>());
			}
		}
	}*/

	public Transform GetTileTransByVector3(Vector3 _pos , LayerMask _layer)
	{
		Vector3 rayOriginPos = _pos + Vector3.up * 20;
		Ray ray = new Ray(rayOriginPos, -Vector3.up);
		RaycastHit castHit;
		
		if (Physics.Raycast(ray, out castHit, 40, _layer))
		{
			Transform tile = castHit.transform;
			
			if (tile != null)
				return tile;
		}
		return null;
	}

	public IntVector2 GetTileIndexByVector3(Vector3 _pos , LayerMask _layer)
	{
		Vector3 rayOriginPos = _pos + Vector3.up * 20;
		Ray ray = new Ray(rayOriginPos, -Vector3.up);
		RaycastHit castHit;

		if (Physics.Raycast(ray, out castHit, 40, _layer))
		{
			Transform tile = castHit.transform;

			if (tile != null)
			{
				if (tile.GetComponent<TileIndex>() != null)
					return tile.GetComponent<TileIndex>().GetIndex();
			}
		}
		return new IntVector2(0,0);
	}

	public void SetMap(Respawn script_respawn)
	{
		validTiles.Clear();
		breakTiles.Clear();

		//Transform tileMark = script_respawn.ef_area;
		//Debug.Log("breakTiles = " + breakTiles.Count);

		//Transform mark = new GameObject("mark").transform;
		

		Ray ray = new Ray();
		RaycastHit castHit = new RaycastHit();
		//LayerMask layerMask = GameObject.FindWithTag("Player").GetComponent<Player>().tileLayer;
		LayerMask layerMask = LayerMask.GetMask(new string[2]{"map","map_exit"/*,"map_etc"*/});

		for (int x = 0; x<MAP_XY; ++x)
		{
			for (int y = 0; y <MAP_XY; ++y)
			{
				ray.origin = new Vector3(x - MAP_XY/2, 40 , y - MAP_XY/2);
				ray.direction = -Vector3.up;

				if (Physics.Raycast(ray, out castHit, 80, layerMask))
				{
					Transform trans = castHit.transform;
					int layer = trans.gameObject.layer;
					//trans.GetComponent<MeshRenderer>().material.renderQueue = 2000;
					switch(layer)
					{
					case 13:
						tiles[x,y] = trans;
						//if (trans.GetComponent<TileIndex>() == null)
						trans.gameObject.AddComponent<TileIndex>().SetIndex(x,y);
						/*Transform a = (Transform)Instantiate(tileMark,castHit.point,trans.rotation);
						a.parent = mark;*/
						validTiles.Add(new IntVector2(x,y));
						break;
					case 17:
						trans.gameObject.AddComponent<TileIndex>().SetIndex(x,y);
						//validTiles.Add(new IntVector2(x,y));
						break;
					case 15:
						exitTileTrans = trans;
						if (trans.GetComponent<TileIndex>() == null)
						{
							trans.gameObject.AddComponent<TileIndex>().SetIndex(x,y);
							script_respawn.SetExit(trans);
						}
						tiles[x,y] = trans;
						break;
					}
				}
			}
		}
		
		layerMask = LayerMask.GetMask(new string[2]{"map_obj","map_break"});
	
		for (int x = 0; x<MAP_XY; ++x)
		{
			for (int y = 0; y <MAP_XY; ++y)
			{
				ray.origin = new Vector3(x - MAP_XY/2, 40 , y - MAP_XY/2);
				ray.direction = -Vector3.up;
				
				if (Physics.Raycast(ray, out castHit, 80, layerMask))
				{
					Transform trans = castHit.transform;
					int layer = trans.gameObject.layer;

					switch(layer)
					{
					case 16:
						script_respawn.AddObj(trans,new IntVector2(x,y));
						break;
					case 14:
						tiles[x,y] = null;
						validTiles.Remove(new IntVector2(x,y));
						trans.name = "_";
						break;
					}
				}
			}
		}
		//mark.gameObject.SetActive (false);
		//script_respawn.SetAreaMark(mark);
	}

	List<IntVector2> innerArea = new List<IntVector2>();
	public void SetInnerArea()
	{
		innerArea.Clear();
		int max = validTiles.Count;
		for (int i = 0; i <max; ++i)
		{
			if (validTiles.Contains(new IntVector2(validTiles[i].x +1 ,validTiles[i].y +1)) == false)
				continue;
			if (validTiles.Contains(new IntVector2(validTiles[i].x -1 ,validTiles[i].y -1)) == false)
				continue;
			if (validTiles.Contains(new IntVector2(validTiles[i].x +1 ,validTiles[i].y -1)) == false)
				continue;
			if (validTiles.Contains(new IntVector2(validTiles[i].x -1 ,validTiles[i].y +1)) == false)
				continue;
			innerArea.Add(validTiles[i]);
		}
	}

	public IntVector2 GetRndInnerArea()
	{
		int rndValue = UnityEngine.Random.Range(0,innerArea.Count);
		IntVector2 rndTile = innerArea[rndValue];
		innerArea.RemoveAt(rndValue);
		validTiles.Remove(rndTile);

		return rndTile;
	}

	public void AddBreakTile(IntVector2 _pos)
	{
		breakTiles.Add(_pos);
		RemoveValidTile(_pos);
	}

	public void RemoveBreakTile(IntVector2 _pos)
	{
		breakTiles.Remove(_pos);
		AddValidTile(_pos);
	}

	public bool  IsBreakTile(IntVector2 _pos)
	{
		return breakTiles.Contains(_pos);
	}

	public IntVector2 GetRndTileIndex()
	{
		int rndIndex = UnityEngine.Random.Range(0,validTiles.Count);
		IntVector2 tile = validTiles[rndIndex];
		RemoveValidTile(tile);
		return tile;
	}

	public bool IsValidTile(IntVector2 _pos)
	{
		return validTiles.Contains(_pos);
	}

	public void AddValidTile(IntVector2 _tile)
	{
		validTiles.Add(_tile);
	}
	public void RemoveValidTile(IntVector2 _tile)
	{
		validTiles.Remove(_tile);
	}

	public void RemoveTile(IntVector2 _tile)
	{
		//tiles[_tile.x,_tile.y].GetComponent<BoxCollider>().enabled = false;
		//Debug.Log(_tile.x + "  " + _tile.y);
		tiles[_tile.x,_tile.y] = null;
		RemoveValidTile(_tile);
	}

	public Transform GetTileTrans(int _x, int _y)
	{
		if (_x >= 0 && _x < MAP_XY && _y>= 0 && _y < MAP_XY)
			return tiles[_x, _y];
		else
			return null;
	}

	public Transform GetTileTrans(IntVector2 _pos)
	{
		if (_pos.x>= 0 && _pos.x < MAP_XY && _pos.y>= 0 && _pos.y < MAP_XY)
			return tiles[_pos.x, _pos.y];
		else
			return null;
	}

	public Vector3 GetTileTransPos(IntVector2 _tile)
	{

		Transform tileTrans = GetTileTrans(_tile);
		if (tileTrans == null)
        {
            Debug.Log("======TileTrans is null : " + _tile.x + ", " + _tile.y);
			return new Vector3(_tile.x,0,_tile.y);
        }
		Vector3 pos = tileTrans.position;
		pos.y = tileTrans.GetComponent<BoxCollider>().bounds.max.y;
		return pos;
	}



	//List<pathNode> closeNode = new List<pathNode>();
	//List<pathNode> openNode = new List<pathNode>();
	//List<IntVector2> nearIndex = new List<IntVector2>();

	public bool PathFinderSimple(IntVector2 _startIndex, IntVector2 _targetIndex)
	{
		Transform a = GameManager.Instance.GetTileTrans(_startIndex);
		Transform b = GameManager.Instance.GetTileTrans(_targetIndex);

		if (a == null || b == null)
			return false;

		int x1 = Mathf.Min(_startIndex.x, _targetIndex.x);
		int x2 = Mathf.Max(_startIndex.x, _targetIndex.x) +1;

		int y1 = Mathf.Min(_startIndex.y, _targetIndex.y);
		int y2 = Mathf.Max(_startIndex.y, _targetIndex.y) +1;

		int gapX = x2 - x1;
		int gapY = y2 - y1;
		
		//Debug.Log("gapX " + gapX);
		//Debug.Log("gapY " + gapY);
		
		for (int x = x1; x<x2; ++x)
		{
			int count = 0;
			for (int y = y1; y<y2; ++y)
			{
				if (GameManager.Instance.GetTileTrans(x,y) == null)
				{
					++ count;
					if (count >= gapY)
						return false;
				}
			}
		}

		for (int y = y1; y<y2; ++y)
		{
			int count = 0;
			for (int x = x1; x<x2; ++x)
			{
				if (GameManager.Instance.GetTileTrans(x,y) == null)
				{
					++ count;
					if (count >= gapX)
						return false;
				}
			}
		}
		return true;
	}

	public int PathFinder(ref IntVector2[] _finalPath, IntVector2 _startIndex, IntVector2 _targetIndex, bool _way8, List<IntVector2> _excludeArea ,int _calculate, bool _justBreakObj, int _gap)
	{
		/*_finalPath.Clear();
		closeNode.Clear();
		openNode.Clear();
		nearIndex.Clear();*/
		//_finalPath.Clear();

		//LinkedList<IntVector2> closeNode = new LinkedList<IntVector2>();
		//LinkedList<IntVector2> openNode = new LinkedList<IntVector2>();
		List<pathNode> closeNode = new List<pathNode>();
		List<pathNode> openNode = new List<pathNode>();
		List<IntVector2> nearIndex = new List<IntVector2>();


		if (_startIndex == _targetIndex)
		{
			return 0;
		}

		pathNode startNode = new pathNode();
		startNode._index = _startIndex;
		startNode._parentIndex = new IntVector2(-1,-1);

		closeNode.Add(startNode); 

		/*pathNode targetNode = new pathNode();
		targetNode._index = startIndex;
		targetNode._parentIndex = new int[]{-1,-1};
		closeNode.Add(targetNode); */

		//Debug.Log("startIndex " + startIndex[0] +" _ "+  startIndex[1]);
		//Debug.Log("targetIndex " + targetIndex[0] +" _ "+  targetIndex[1]);

		//if (_startIndex.x == 0 && _startIndex.y == 0)
		//	return 0;

		for (int k = 0; k<_calculate; ++k)
		{
			pathNode lastCloseNode = closeNode[closeNode.Count -1];
			if (IntVector2.DistanceCross(lastCloseNode._index,_targetIndex) == _gap)
			{
				//Debug.Log("Success !!!!!!!!!!!!!");
				closeNode.Reverse();

				pathNode tempNode = closeNode[0];
				int count = 0;
				_finalPath[count] = tempNode._index;
				++count;
				int max = closeNode.Count;
				for (int i = 0; i< max; ++i)
				{
					if (tempNode._parentIndex.x < 0 || tempNode._parentIndex.y < 0)
						break;
					
					pathNode tempNode1 = FindParentNode(tempNode, closeNode);

					if (_way8 == false)
					{
						if (tempNode1._parentIndex.x < 0 || tempNode1._parentIndex.y < 0)
						{
							tempNode = tempNode1;
						}
						else
						{
							pathNode tempNode2 = FindParentNode(tempNode1, closeNode);
							if (tempNode._index.x != tempNode2._index.x   && tempNode._index.y != tempNode2._index.y)
								tempNode = tempNode2;
							else
								tempNode = tempNode1;
						}
					}
					else
					{
						tempNode = tempNode1;
					}
					
					_finalPath[count] = tempNode._index;
					++count;

                    if(_finalPath.Length <= count + 1)
                    {
                        break;
                    }
				}

				return count;
			}


			nearIndex.Clear();
			float plus_distance = 1;
			if (_startIndex.x < MAP_XY -1)
			{
				IntVector2 aa = new IntVector2(lastCloseNode._index.x +1, lastCloseNode._index.y);
				nearIndex.Add(aa);
			}
			if (_startIndex.x > 0)
			{
				IntVector2 aa = new IntVector2(lastCloseNode._index.x -1, lastCloseNode._index.y);
				nearIndex.Add(aa);
			}
			if (_startIndex.y < MAP_XY -1)
			{
				IntVector2 aa = new IntVector2(lastCloseNode._index.x, lastCloseNode._index.y +1);
				nearIndex.Add(aa);
			}
			if (_startIndex.y > 0)
			{
				IntVector2 aa = new IntVector2(lastCloseNode._index.x , Mathf.Max(0, lastCloseNode._index.y -1));
				nearIndex.Add(aa);
			}
			if(_way8)
			{
				if (_startIndex.x < MAP_XY -1)
				{
					IntVector2 aa = new IntVector2(lastCloseNode._index.x +1, lastCloseNode._index.y+1);
					nearIndex.Add(aa);
					plus_distance = 1.414f;
				}
				if (_startIndex.x > 0)
				{
					IntVector2 aa = new IntVector2(lastCloseNode._index.x -1, lastCloseNode._index.y+1);
					nearIndex.Add(aa);
					plus_distance = 1.414f;
				}
				if (_startIndex.y < MAP_XY -1)
				{
					IntVector2 aa = new IntVector2(lastCloseNode._index.x+1, Mathf.Max(0,lastCloseNode._index.y -1));
                    nearIndex.Add(aa);
					plus_distance = 1.414f;
				}
				if (_startIndex.y > 0)
				{
					IntVector2 aa = new IntVector2(lastCloseNode._index.x -1, Mathf.Max(lastCloseNode._index.y -1));
                    nearIndex.Add(aa);
					plus_distance = 1.414f;
				}
			}

			if (_excludeArea != null)
			{
				if (_excludeArea.Count>0)
				{
					for (int i = 0; i<_excludeArea.Count; ++i)
					{
						nearIndex.Remove(_excludeArea[i]);
					}
				}
			}

			if (_justBreakObj == false)
			{
				for (int i = 0; i<breakTiles.Count; ++i)
				{
					//if (breakTiles[i] != _targetIndex)
						nearIndex.Remove(breakTiles[i]);
				}
			}

			for (int i = 0; i<nearIndex.Count; ++i)
			{
                if (nearIndex[i].x >= 60 
                    || nearIndex[i].y >= 60
                    || nearIndex[i].x < 0
                    || nearIndex[i].y < 0
                    )
                {
                    continue;
                }
                if (tiles[nearIndex[i].x,nearIndex[i].y] == null)
					continue;
				
				if (NodeContain(openNode, nearIndex[i]) == false && NodeContain(closeNode, nearIndex[i]) == false)
				{
					pathNode addOpenNode = new pathNode();
					addOpenNode._index = nearIndex[i];
					addOpenNode._parentIndex = lastCloseNode._index;
					addOpenNode._distance = lastCloseNode._distance + plus_distance;
					openNode.Add(addOpenNode); 
				}
			}

			//Debug.Log("ccc " + openNode.Count);
			if (openNode.Count >0)
			{
				float minValue = 3600;
				int minIndex = 0;
				for (int i = 0; i<openNode.Count; ++i)
				{
					float distance = openNode[i]._distance + IntVector2.DistanceCross(openNode[i]._index, _targetIndex);
					/*float distance = MathPow(openNode[i]._index.x - _targetIndex.x) + MathPow(openNode[i]._index.y - _targetIndex.y) 
						+MathPow(openNode[i]._index.x  - _startIndex.x) + MathPow(openNode[i]._index.y - _startIndex.y);*/
					if (distance < minValue)
					{
						minValue = distance;
						minIndex = i;
					}
				}

				closeNode.Add(openNode[minIndex]);
				openNode.RemoveAt(minIndex);
			}
			else
			{
				if (_gap == 1)
					return -1;
				else
					return 0;
			}
		}

		//Debug.Log("finish _ short");
		if (_gap == 1)
			return -1;
		else
			return 0;
	}

	int MathPow(int _value)
	{
		return _value * _value;
	}

	IntVector2 FindSelectPanelIndex(Transform _trans)
	{
		if (_trans.GetComponent<TileIndex>() != null)
			return _trans.GetComponent<TileIndex>().GetIndex();
		else
			return new IntVector2();
	}

	bool NodeContain(List<pathNode> _list, IntVector2 _index)
	{
		for (int i = 0; i<_list.Count; ++i)
		{
			if (_list[i]._index == _index)
			{
				return true;
			}
		}
		return false;
	}

	pathNode FindParentNode(pathNode _node , List<pathNode> _nodeList)
	{
		for (int i = 0; i<_nodeList.Count; ++i)
		{
			if(_nodeList[i]._index == _node._parentIndex)
				return _nodeList[i];
		}
		return _nodeList[0];
	}

	public IEnumerator ShakeTrans(Transform _trans, Vector3 _shakeDir, int _speed, int _count , Action _action, bool _disableInput = false , float _reduceFactor = -0.5f)
	{
		//Debug.Log("shake !!!! ");
		if(_disableInput == true)
			GameManager.Instance.DisableInput(true);
		Vector3 originPos = _trans.localPosition;

		for (int i = 0; i < _count; ++i)
		{
			float dt = 1.0f /_speed;
			Vector3 targetPos = originPos + _shakeDir;
			while (dt > 0)
			{
				_trans.localPosition = Vector3.Lerp(_trans.localPosition, targetPos, Time.deltaTime * _speed);
				dt -= Time.deltaTime;
				yield return null;
			}
			_shakeDir *= _reduceFactor;
		}

		_trans.localPosition = originPos;
		if(_action != null)
			_action.Invoke();
		if(_disableInput == true)
			GameManager.Instance.DisableInput(false);
	}

	public void ShatterObj(Transform _trans /*, Shader _shader*/, int _force)
	{
		if(_trans == null)
			return;
		//Transform shatterTrans = (Transform)Instantiate(_trans,_trans.position,_trans.rotation);
		//shatterTrans.gameObject.layer = 0;
		//Destroy(_trans.gameObject);

		//Destroy(_trans.GetComponent<BoxCollider>());
		GameObject obj = _trans.gameObject;
		//Mesh a = obj.GetComponent<MeshFilter>().mesh;
		//UnityEditor.MeshUtility.Optimize(a);
		//shatterTrans.GetComponent<MeshRenderer>().material.shader = _shader;
		obj.layer = 18;

		ShatterTool shatter = obj.AddComponent<ShatterTool>();
		obj.AddComponent<TargetUvMapper>();
		//MeshCollider col = obj.AddComponent<MeshCollider>();
		//col.convex = true;
		if (obj.GetComponent<BoxCollider>() == null)
			obj.AddComponent<BoxCollider>();

		Debug.Log("aaa " + obj.name);
		Rigidbody rigidBody = obj.AddComponent<Rigidbody>();
		rigidBody.drag = 1;
		StartCoroutine(ShatterStart(shatter, obj, _force));

	}

	IEnumerator ShatterStart(ShatterTool _shatter ,GameObject _obj ,int _force)
	{
		//Debug.Log("shatter obj : "+_obj.name);
		yield return null;
		GameObject parentNode = new GameObject("node");
		Transform parentNodeTrans = parentNode.transform;

		//Vector3 pos = _obj.transform.position + _obj.GetComponent<MeshFilter>().mesh.bounds.center;
		Vector3 pos = _obj.transform.position ; 
		parentNodeTrans.position = _obj.GetComponent<MeshFilter>().mesh.bounds.center + pos;

		_shatter.Shatter(pos + Vector3.up *0.2f  , parentNodeTrans ,_force);

		yield return new WaitForSeconds(1.6f);

		for (int i = 0; i<parentNodeTrans.childCount; ++i)
		{
			parentNodeTrans.GetChild(i).GetComponent<BoxCollider>().enabled = false;
		}
		float delay = 1;
	
		/*while(delay >0)
		{
			parentNodeTrans.position -= Vector3.up * Time.deltaTime * 0.3f;
			delay -= Time.deltaTime;
			yield return null;
		}*/
		Destroy(parentNode,1.0f);
	}

	public string RefreshLanguageIcon()
	{
		string flagName = "icon_kr";
		switch(Localization.language)
		{
		case "English":
			flagName = "icon_us";
			break;
		case "Japanese":
			flagName = "icon_jp";
			break;
		case "Korean":
			flagName = "icon_kr";
			break;
		case "Tagalog":
			flagName = "icon_ph";
			break;
		case "Chinese1":
			flagName = "icon_cn";
			break;
		case "Chinese2":
			flagName = "icon_cn";
			break;
		case "Indonesian":
			flagName = "icon_in";
			break;
		case "Thai":
			flagName = "icon_tl";
			break;
		}
		return flagName;
	}

	bool initFinish = false;
	public void SetInit()
	{
		initFinish = true;
	}
	public bool GetInit()
	{
		return initFinish;
	}

	public void SetLanguage(int _index)
	{
		switch (_index)
		{
		case 1:
			Localization.language = "Korean";
			break;
		case 2:
			Localization.language = "English";
			break;
		case 3:
			Localization.language = "Japanese";
			break;
		case 4:
			Localization.language = "Tagalog";
			break;
		case 5:
			Localization.language = "Chinese1";
			break;
		case 6:
			Localization.language = "Chinese1";
			break;
		case 7:
			Localization.language = "Indonesian";
			break;
		case 8:
			Localization.language = "Thai";
			break;
		case 9:
			Localization.language = "Korean";
			break;
		}
	}


	IEnumerator Quit()
	{
		yield return new WaitForSeconds(1.5f);
		Application.Quit();
	}


	void OnObscuredCheatingDetected()
	{
		Debug.LogError("OnObscuredCheatingDetected!!!!");
		Application.Quit();
	}
	void OnInjectionDetected()
	{
		Debug.LogError("OnInjectionDetected!!!!");
		//Application.Quit();
	}
	void OnSpeedHackDetected()
	{
		Debug.LogError("OnSpeedHackDetected!!!!");
		//Application.Quit();
	}
	void OnWallHackDetected()
	{
		Debug.LogError("OnWallHackDetected!!!!");
		//Application.Quit();
	}

	void Start() 
	{
		//UnityEditor.EditorPrefs.SetBool("ACTDIDEnabledGlobal", true);
		//ObscuredCheatingDetector.StartDetection(OnObscuredCheatingDetected);
		//InjectionDetector.StartDetection(OnInjectionDetected);
		//SpeedHackDetector.StartDetection(OnSpeedHackDetected);
		//WallHackDetector.StartDetection(OnWallHackDetected);

		Screen.sleepTimeout = SleepTimeout.NeverSleep;
		Application.targetFrameRate = 60;
	}


	public void SetMainCamBloom(bool _on)
	{
		if (VUserManager.Instance.IsStage() == true)
			return;

		BloomOptimized mainBloom = Camera.main.GetComponent<BloomOptimized>();
		if (mainBloom != null)
			mainBloom.enabled = _on;
	}
	Coroutine bloomCoroutine;
	public void SetUICamBloomTween(float _delay = 0)
	{
		SetMainCamBloom(false);
		if (bloomCoroutine != null)
			StopCoroutine(bloomCoroutine);
		bloomCoroutine = StartCoroutine(SetUICamBloomTween_Coroutine(_delay));
	}

	IEnumerator SetUICamBloomTween_Coroutine(float _delay)
	{
		float intensity = 2.5f;

		BloomOptimized bloom = GetCamUI().GetComponent<BloomOptimized>();
		if (bloom == null)
			yield break;

		bloom.enabled = true;
		bloom.intensity = intensity;

		if (_delay>0)
			yield return new WaitForSeconds(_delay);
		while(intensity > 0)
		{
			bloom.intensity = intensity;
			intensity -= Time.deltaTime * 5;
			yield return null;
		}
		bloom.enabled = false;
	}

	public void ScaleTweenUI(Transform _ui)
	{
		StartCoroutine(ScaleTweenUI_Loop(_ui));
	}

	IEnumerator ScaleTweenUI_Loop(Transform _ui)
	{
		//_ui.gameObject.SetActive( true );
		_ui.localScale = Vector3.one;
		float delay = 0.05f;
		Vector3 targetScale = Vector3.one*1.05f;
		while (delay > 0)
		{
			delay -= Time.deltaTime;
			if (_ui.gameObject.activeInHierarchy == false)
				yield break;
			else
			{
				_ui.localScale = Vector3.Lerp (_ui.localScale,targetScale,Time.deltaTime* 3);
				yield return null;
			}
		}
		delay = 0.05f;
		while (delay > 0)
		{
			delay -= Time.deltaTime;
			if (_ui.gameObject.activeInHierarchy == false)
				yield break;
			else
				_ui.localScale = Vector3.Lerp (_ui.localScale,Vector3.one,Time.deltaTime* 3);
			yield return null;
		}
		_ui.localScale = Vector3.one;
	}


	public void SetUIcamLayer(int _layer)
	{
		//if (guiCam == null)
		//	guiCam = GetRootUI().transform.GetChild(0).GetComponent<UICamera>();

		//guiCam.GetComponent<Camera>().depth = _layer;
	}

	bool networkInputLock = false;
	bool clientInputLock = false;


	public void DisableInput(bool _disable)
	{
		//if (guiCam == null)
		//	guiCam = GetRootUI().transform.GetChild(0).GetComponent<UICamera>();

		clientInputLock = _disable;

		bool activeInput = true;

		if (clientInputLock == true)
		{
			activeInput = false;
		}
		if (networkInputLock == true)
		{
			activeInput = false;
		}


		GetCamUI().useMouse = activeInput;
		GetCamUI().useTouch = activeInput;
	}

	public void SetNetworkInputLock(bool _disable)
	{
		//if (guiCam == null)
		//	guiCam = GetRootUI().transform.GetChild(0).GetComponent<UICamera>();

		networkInputLock = _disable;

		bool activeInput = true;
		
		if (clientInputLock == true)
		{
			activeInput = false;
		}
		if (networkInputLock == true)
		{
			activeInput = false;
		}

		GetCamUI().useMouse = activeInput;
		GetCamUI().useTouch = activeInput;
		//guiCam.useMouse = activeInput;
		//guiCam.useTouch = activeInput;
	}

	public void SetNetWorking(bool _on)
	{
		netWorkIng = _on;

		if (netWorkIngToggle == true && _on == false)
		{
			return;
		}

		SetNetworkInputLock(_on);
		LoadingMarkOn(_on);
	}

	public void SetNetworkToggle(bool _on)
	{
		netWorkIngToggle = _on;

		if (netWorkIng == true && _on == false)
		{
			return;
		}

		SetNetworkInputLock(_on);
		LoadingMarkOn(_on);
	}

	public bool GetNetWorking()
	{
		return netWorkIng;
	}
	public bool GetNetWorkToggle()
	{
		return netWorkIngToggle;
	}


	bool loadSceneOn = false;
	public string nextSceneName = "Camp";

	string txt_tip = "";
	public string GetTip()
	{
		return txt_tip;
	}
	void SetTip()
	{
		txt_tip = "tip_"+ nextSceneName +"_"+  UnityEngine.Random.Range(0,10);
		if (Localization.Exists(txt_tip) == false)
			txt_tip = "tip_House_"+  UnityEngine.Random.Range(0,10);
	}

	public void MoveScene (string _sceneName , bool _directMove , Action _action)
	{
		nextSceneName = _sceneName;
		VUserManager.Instance.OnMoveScene(nextSceneName);

		if(_directMove == false)
			StartCoroutine(MoveScene_Coroutine(_action));
		else
			SceneManager.LoadScene(nextSceneName);

		for (int i = 0; i<selectTabSpr.Count; ++i)
		{
			if (selectTabSpr[i] != null)
				Destroy(selectTabSpr[i].gameObject);
		}
		selectTabSpr.Clear();
	}

	public IEnumerator MoveStage_Coroutine(Action _action)
	{
		UI_Control.Instance.CloseAllUI();
		DisableInput(true);

		Transform bg_loading = transform.GetChild(0);
		Renderer bg_loadingRenderer = bg_loading.GetComponent<MeshRenderer>();
		bg_loading.gameObject.SetActive(true);
		
		float alpha = 0;
		Color color = new Color(0,0,0,alpha);
		while(alpha < 1)
		{
			alpha += Time.deltaTime;
			color.a = alpha;
			bg_loadingRenderer.material.SetColor("_TintColor",color);
			yield return null;
		}
		if(_action != null)
			_action.Invoke();
	}

	IEnumerator MoveScene_Coroutine(Action _action)
	{
		if (loadSceneOn)
			yield break;
		loadSceneOn = true;
		
		UI_Control.Instance.CloseAllUI();
		DisableInput(true);

		SetTip();

		Transform bg_loading = transform.GetChild(0);
		Renderer bg_loadingRenderer = bg_loading.GetComponent<MeshRenderer>();
		bg_loading.gameObject.SetActive(true);

		float alpha = 0;
		Color color = new Color(0,0,0,alpha);
		while(alpha < 1)
		{
			alpha += Time.deltaTime;
			color.a = alpha;
			bg_loadingRenderer.material.SetColor("_TintColor",color);
			yield return null;
		}
		if(_action != null)
			_action.Invoke();
		
		if (string.IsNullOrEmpty(nextSceneName) == false)
		{
			//DisableInput(false);
			SceneManager.LoadScene("Loading");
			UI_Control.Instance.ClearData();
			SetLoadingGauge(0,nextSceneName);
		}
	}

	public void LoadSceneFinish()
	{
		loadSceneOn = false;
		Resources.UnloadUnusedAssets();
		StartCoroutine(LoadSceneFinish_Coroutine());
	}

	IEnumerator LoadSceneFinish_Coroutine()
	{
		Transform bg_loading = transform.GetChild(0);
		Renderer bg_loadingRenderer = bg_loading.GetComponent<MeshRenderer>();
		bg_loading.gameObject.SetActive(true);

		float alpha = 1;
		Color color = new Color(0,0,0,alpha);
		while(alpha > 0)
		{
			alpha -= Time.deltaTime;
			color.a = alpha;
			bg_loadingRenderer.material.SetColor("_TintColor",color);
			yield return null;
		}

		DisableInput(false);
		bg_loading.gameObject.SetActive(false);
	}

	public void InitSave (string charNickName)
	{

	}

	public void NewStart(string _nickname)
	{

	}

    public void IntroStart()
    {
		if (VUserManager.Instance.isExistSaveData() == false)
		{
			MoveScene("Camp", false, null);
		}
		else
		{
			if (VUserManager.Instance.IsValidData() == false)
			{
				VUserManager.Instance.LoadData();
				VPlayerManager.Instance.LoadData();
                VMUnitElement.Instance.LoadData();
				VItemManager.Instance.LoadAllData();
				VEquipManager.Instance.LoadAllData();
				VMPartySkill.Instance.LoadData();
                VFieldManager.Instance.LoadData();
                VGDungeonManager.Instance.LoadData();
            }
			if (VUserManager.Instance.m_userInfo != null)
				VUserManager.Instance.m_userInfo.RefreshData();

			if (VFieldManager.Instance.GetFieldAID() != 0)
				MoveScene("Stage", false, null);
			else
				MoveScene("Camp", false, null);
		}
    }

	public void LoadingMarkOn(bool _on)
	{
		if (mark_loading == null)
		{
			mark_loading = ResourcesLoadGameObject("UI/mark_loading");
		}
		if (mark_loading != null)
		{
			mark_loading.gameObject.SetActive(_on);
		}
	}

	public Transform ResourcesLoadGameObject(Transform _parent, string _name)
	{
		GameObject go = Resources.Load(_name)as GameObject;
		if (go == null)
			return null;
		else
		{
			Transform trans = (Transform)Instantiate (go.transform,Vector3.zero,Quaternion.identity);
			if (_parent != null)
				trans.parent = _parent;
			trans.localPosition = go.transform.localPosition;
			trans.localRotation = go.transform.localRotation;
			return trans;  
		}
	}

	public Transform ResourcesLoadGameObject(string _name)
	{
		return ResourcesLoadGameObject(null,_name);
	}

	public Transform ResourcesLoadGUI(GameObject _parent, string _name)
	{
		GameObject go = Resources.Load(_name)as GameObject;
		if (go == null)
			return null;
		else
		{
			Transform trans = NGUITools.AddChild(_parent,go).transform;
			trans.localPosition = go.transform.localPosition;
			return trans;
		}
	}

	public Transform ResourcesLoadGUI(string _name)
	{
		return ResourcesLoadGUI (GetRootUI(),_name);
	}

	public int GetDiffDay(DateTime inRewardDate, DateTime inNowDate)
	{
		DateTime reward_date = new DateTime(inRewardDate.Year, inRewardDate.Month, inRewardDate.Day);
		DateTime now_date = new DateTime(inNowDate.Year, inNowDate.Month, inNowDate.Day);
		TimeSpan diff_date = reward_date.Subtract(now_date);
		
		return diff_date.Days;
	}

	public Transform FindChild_ActiveTrans(Transform _parent, Vector3 _pos)
	{
		for (int i = 0; i<_parent.childCount; ++i)
		{
			Transform child = _parent.GetChild(i);
			if (child.gameObject.activeInHierarchy == true)
			{
				if(Mathf.Abs(_pos.x - child.position.x) <0.5f && Mathf.Abs(_pos.z - child.position.z) <0.5f)
					return child;
			}
		}
		return null;
	}

	public Transform FindChild_InactiveTrans(Transform _parent)
	{
		for (int i = 0; i<_parent.childCount; ++i)
		{
			Transform child = _parent.GetChild(i);
			if (child.gameObject.activeSelf == false)
			{
				return child;
			}
		}
		return null;
	}

	public void HideAllChild(Transform _parent)
	{
		for (int i = 0; i<_parent.childCount; ++i)
		{
			_parent.GetChild(i).gameObject.SetActive(false);
		}
	}

	public IEnumerator TxtAnimation(Transform _txtSet , ParticleEmitter _particle , int _startSize)
	{
		int max = _txtSet.childCount;
		//Time.timeScale = 1;
		_txtSet.gameObject.SetActive(true);

		bool[] move = new bool[max];
		float[] delay = new float[max];
		float delayOg = 0.5f;
		//float addDelay = 0.
		for (int i = 0; i<max; ++i)
		{
			Transform txt = _txtSet.GetChild(i);
			txt.localScale = Vector3.one * _startSize;
			txt.gameObject.SetActive(false);
			delayOg += 0.4f / max;
			delay[i] = delayOg ;
		}


		//Transform txt = _txtSet.GetChild(i);


		int finishCount = max;
		while(finishCount > 0)
		{
			for (int i = 0; i<max; ++i)
			{
				if (move[i] == false)
				{
					Transform txt = _txtSet.GetChild(i);
					if (txt.gameObject.activeInHierarchy == false)
					{
						if (delay[i] <0)
						{
							txt.gameObject.SetActive(true);
						}
						else
						{
							delay[i] -= Time.deltaTime;
							continue;
						}
					}

					if (txt.localScale.x >1)
					{
						txt.localScale -= Vector3.one * Time.deltaTime * (10 + max) ;
					}
					else
					{
						txt.localScale = Vector3.one;
						move[i] = true;
						-- finishCount ;

						if (_particle != null)
						{
							_particle.transform.localPosition = txt.localPosition;
							_particle.Emit();
						}
					}
				}
			}
			yield return null;
		}
	}

	string packageName = "";
	public void SetPackageName(string _name)
	{
		packageName = _name;
	}

	public string GetPackageName()
	{
		return packageName;
	}

	public Vector3 SetIconArrayPosXY (int _total, int _maxX, int _index, float _spanX, float _spanY)
	{
		int a = _total/_maxX;
		
		int row = 0;
		if ( _index <  a * _maxX )
			row = Mathf.Min(_maxX, _total);
		else
			row = Mathf.Min(_maxX, _total %_maxX);

		int column = (_total -1) / _maxX;
		Vector3 pos = new Vector3 ( (row - 1) * _spanX /-2 + (_index %_maxX) * _spanX , (column) * _spanY /2  - (_index / _maxX ) * _spanY ,0);
		return pos;
	}

	public Vector3 SetIconArrayPosXY2 (int _total, int _maxX, int _index, float _spanX, float _spanY)
	{
		int a = _total/_maxX;

		int row = Mathf.Min(_maxX, _total);
		
		int column = (_total -1) / _maxX;
		Vector3 pos = new Vector3 ( (row - 1) * _spanX /-2 + (_index %_maxX) * _spanX , (column) * _spanY /2  - (_index / _maxX ) * _spanY ,0);
		return pos;
	}

	public Vector3 SetIconArrayPosX (int _total, int _index, float _span)
	{
		return Vector3.right * ( (_total -1) * _span /-2 + _index * _span);
	}

	public Vector3 SetIconArrayPosY (int _total, int _index, int _span)
	{
		return Vector3.up * -( (_total -1) * _span /-2 + _index * _span);
	}

	public Vector3 SetIconArrayPosX2 ( int _index, float _span ,int _startX)
	{
		return Vector3.right * ( _index * _span +_startX);
	}

	public Vector3 SetIconArrayPosY2 (int _total, int _index, int _span)
	{
		return Vector3.up * -( _index * _span);
	}

	public void PanelFadeOut(UIPanel _panel ,int _speed)
	{
		StartCoroutine(PanelFadeOut_Loop(_panel, _speed));
	}

	IEnumerator PanelFadeOut_Loop(UIPanel _panel , int _speed)
	{
		//DisableInput(true);

		while(_panel.alpha > 0)
		{
			_panel.alpha -= Time.deltaTime *_speed;
			yield return null;
		}

		_panel.alpha = 1;
		_panel.gameObject.SetActive(false);
		//DisableInput(false);
	}

	public void PanelFadeIn(UIPanel _panel ,int _speed)
	{
		StartCoroutine(PanelFadeIn_Loop(_panel, _speed));
	}

	IEnumerator PanelFadeIn_Loop(UIPanel _panel , int _speed)
	{
		//DisableInput(true);
		_panel.gameObject.SetActive(true);
		_panel.alpha = 0;
		while(_panel.alpha < 1)
		{
			_panel.alpha += Time.deltaTime *_speed;
			yield return null;
		}

		_panel.alpha = 1;
		//DisableInput(false);
	}

	List<Transform> selectTabSpr = new List<Transform>();
	public void RefreshTab() // 탭  gameObject 가 destory 된 리스트 remove 로 정리.
	{
		List<Transform> removeList = selectTabSpr.FindAll(m => m == null);
		
		for (int i = 0; i<removeList.Count; ++i)
		{
			selectTabSpr.Remove(removeList[i]);
		}
		removeList.Clear();
	}

	void SelectTabSpr(Transform _root, Transform _bg)
	{
		if (_bg == null)
		{
			for (int i = 0; i<selectTabSpr.Count; ++i)
			{
				selectTabSpr[i].parent = GetRootUI().transform;
				selectTabSpr[i].gameObject.SetActive(false);
			}
			return;
		}
		
		Transform curSprTrans = null;
		for (int i = 0; i<selectTabSpr.Count; ++i)
		{
			if (selectTabSpr[i].parent.parent == _root)
			{
				curSprTrans = selectTabSpr[i];
				break;
			}
		}
		if (curSprTrans == null)
		{
			for (int i = 0; i<selectTabSpr.Count; ++i)
			{
				if (selectTabSpr[i].gameObject.activeInHierarchy == false)
				{
					curSprTrans = selectTabSpr[i];
					break;
				}
			}
		}
		
		if (curSprTrans == null)
		{
			curSprTrans = ResourcesLoadGUI(_bg.parent.gameObject,"UI/selectTab");
			selectTabSpr.Add(curSprTrans);
		}
		else
		{
			curSprTrans.parent = _bg.parent;
		}

		UISprite spr = curSprTrans.GetComponent<UISprite>();
		UISprite bg_spr = _bg.GetComponent<UISprite>();

		spr.SetDimensions(bg_spr.width,bg_spr.height);
		spr.depth = bg_spr.depth+1;
		//curSprTrans.gameObject.SetActive(true);
		curSprTrans.localPosition = _bg.localPosition;
		StartCoroutine(InitScaleGameObject(curSprTrans.gameObject));
	}


	public void SetTab(Transform _tabs, int _selectIndex)
	{
		if (_tabs == null)
			return;
		for (int i = 0; i<_tabs.childCount; ++i)
		{
			Transform tab = _tabs.GetChild(i);
			UILabel txt = tab.GetChild(0).GetComponent<UILabel>();
			Transform icon = tab.Find("icon");
			
			if (i == _selectIndex)
			{
				txt.color = Color.black;
				Transform bg = tab.Find("bg");
				if (icon != null)
					icon.GetComponent<UISprite>().color = Color.black;
				SelectTabSpr(_tabs,bg);
			}
			else
			{
				txt.color = Color.white;
				if (icon != null)
					icon.GetComponent<UISprite>().color = Color.white;
			}
		}
	}

	public void SetTabNull(Transform _tabs)
	{
		for (int i = 0; i<_tabs.childCount; ++i)
		{
			Transform tab = _tabs.GetChild(i);
			UILabel txt = tab.GetChild(0).GetComponent<UILabel>();
			txt.color = Color.white;
		}
		SelectTabSpr(null,null);
	}

	public IEnumerator InitScaleGameObject(GameObject _go)
	{
		yield return null;
		_go.transform.localScale = Vector3.one;
	}
	public IEnumerator InitActiveGameObject(GameObject _go)
	{
		_go.SetActive(false);
		yield return null;
		_go.SetActive(true);
	}
	public IEnumerator ActiveGameObjectDuration(GameObject _go , float _duration, float _delay = 0)
	{
		if (_delay >0)
		{
			_go.SetActive(false);
			yield return new WaitForSeconds(_delay);
		}

		_go.SetActive(true);
		yield return new WaitForSeconds(_duration);
		_go.SetActive(false);
	}


	public void DestroyObj(GameObject _obj)
	{
		Destroy(_obj);
		StartCoroutine(UnloadUnuse());
	}
	public IEnumerator UnloadUnuse()
	{
		yield return null;
		Resources.UnloadUnusedAssets();
	}

	public IEnumerator PointAnimation(UILabel _label, float _delay, float _duration, float _curValue, float _targetValue ,string addStr = "")
	{
		_label.text = ((int)_curValue).ToString();
		float gap = Mathf.Abs(_targetValue - _curValue);
		float addPoint = 1;
		if (gap <1)
			addPoint =  1; 
		else
			addPoint =  gap /  (_duration ); 

		yield return new WaitForSeconds(_delay);
		_label.transform.localScale = Vector3.one *1.5f;
		while (_curValue !=_targetValue)
		{
			_curValue = Mathf.MoveTowards(_curValue,_targetValue, addPoint * Time.deltaTime);
			_label.text = ((int)_curValue).ToString() + addStr;
			/**if (_label.animation != null)
			{
				_label.animation.Play();
			}*/
			yield return null;
		}
		_label.transform.localScale = Vector3.one;
		_label.text = ((int)_targetValue).ToString() + addStr;
	}

	public IEnumerator GaugeAnimationRatio(UIProgressBar _gauge, float _delay, float _speed, float _targetRatio, int _overCount , UILabel _label)
	{
		float curValue = 0;
		_gauge.value = curValue;

		yield return new WaitForSeconds(_delay);

		float targetValue = 0;
		_label.text = "0";
		for (int i = 0 ; i<_overCount + 1; ++i)
		{
			curValue = 0;
			if (i == _overCount )
				targetValue = _targetRatio;
			else
				targetValue = 1;
			while (curValue < targetValue)
			{
				curValue += _speed * Time.deltaTime;
				_gauge.value = curValue;
				yield return null;
			}
			_label.text = i.ToString();
			if(_label.GetComponent<Animation>() != null)
			{
				_label.GetComponent<Animation>().Stop();
				_label.GetComponent<Animation>().Play();
			}
		}

		_gauge.value = _targetRatio;
	}

	public IEnumerator GaugeAnimation(UIProgressBar _gauge, float _delay, float _duration, float _curValue, float _targetValue, int _overCount , System.Action<long> _action , long _arg )
	{
		_gauge.value = _curValue;
		float addPoint =  (_targetValue +_overCount - _curValue) / _duration; 

		yield return new WaitForSeconds(_delay);
		while (_curValue <_targetValue + _overCount)
		{
			_curValue += addPoint * Time.deltaTime;
			if (_curValue >1)
			{
				_curValue -=1;
				_overCount -= 1;
				if (_action !=null)
					_action.Invoke(_arg);
			}
			_gauge.value = _curValue;
			yield return null;
		}
		_gauge.value = _targetValue;
	}

	public void ApplicationQuit()
	{
		if (loadSceneOn)
			return;

		CommonPopupManager.Instance.StartPopup(CommonPopupManager.ePopupType.YESNO, Localization.Get ("confirm_quit"),ApplicationQuit_Confirm,null);	
		
		/*if (set_quit == null)
		{
			set_quit = NGUITools.AddChild(GetRootUI(), Resources.Load("UI/set_quit")as GameObject);
			set_quit.SetActive(true);
		}
		else
		{
			if (!set_quit.activeInHierarchy)
			{
				set_quit.SetActive(true);
			}
			else
			{
				set_quit.SetActive(false);
			}
		}*/
	}
	public void ApplicationQuit_Confirm()
	{
		Application.Quit();
	}

	void OnApplicationFocus(bool _on) 
	{

	}

	void OnApplicationQuit()
	{

		s_instance = null;
	}
		
		
	public string ToHexString(int nor)
	{
		byte[] bytes = BitConverter.GetBytes(nor);
		string hexString = "";
		for(int i=0; i <1; i++){
			hexString += bytes[i].ToString("X2");
		}
		return hexString;
	}

	public string GetResourceText (string _path)
	{
		TextAsset txtAsset = Resources.Load(_path) as TextAsset;
		if (txtAsset != null)
		{
			if (txtAsset.text.Length > 0) return txtAsset.text;
		}
		return "";
	}



	public string GetCountryCode()
	{
		try
		{
			string iso = "kr";

			#if UNITY_ANDROID
			AndroidJavaObject TM = new AndroidJavaObject("android.telephony.TelephonyManager");
			
			iso = TM.Call<string>("getNetworkCountryIso");
			if (string.IsNullOrEmpty (iso)) {
				iso = TM.Call<string>("getSimCountryIso");
			}
			
			Debug.Log("current lang network = " + iso); 
			#elif UNITY_IPHONE
			iso = IOS_Utils.IPhoneCountryCode();
			#endif
			return iso;
		}
		catch( Exception ex)
		{
			Debug.Log( "[GameManager] GetCountryCode() Exception:" + ex);
			return "";
		}
	}

	public Transform CreatCustomPlane(Material _mat) 
	{
		GameObject c_plane = new GameObject("item");
		MeshFilter meshFilter = c_plane.AddComponent<MeshFilter>();
		Mesh c_mesh =  new Mesh();

		c_plane.AddComponent<MeshRenderer>();  
		
		c_mesh.vertices = new Vector3[]
		{
			new Vector3(-1, 1, 0) , new Vector3(1, 1, 0),             
			new Vector3(-1, -1, 0), new Vector3(1, -1, 0)      
		};
		
		/*c_mesh.uv = new Vector2[]
		{
			new Vector2(_leftbottomUV.x, _rightupUV.y), new Vector2(_rightupUV.x, _rightupUV.y),      
			new Vector2(_leftbottomUV.x, _leftbottomUV.y), new Vector2(_rightupUV.x, _leftbottomUV.y)     
		};*/
		
		Renderer renderer= c_plane.GetComponent<Renderer>();
		
		renderer.receiveShadows = false;
		renderer.castShadows = false;
		renderer.sharedMaterial = _mat;
		
		c_mesh.triangles = new int[]{0,1,2,2,1,3};              
		c_mesh.RecalculateNormals();                  
		meshFilter.mesh = c_mesh;    

		//c_plane.layer = gameObject.layer;
		
		return c_plane.transform;
		//meshcollider.sharedMesh = mesh;
	}

	public void StartUIDelay (eGUISetID targetId)
	{
		StartCoroutine(StartUIDelay_Coroutine(targetId));
	}

	IEnumerator StartUIDelay_Coroutine (eGUISetID targetId)
	{
		yield return null;
		UI_Control.Instance.StartUI(targetId);
	}

	public void LastUIOpen(eGUISetID _uiID)
	{
		StartCoroutine(LastUIOpen_Coroutine(_uiID));
	}

	IEnumerator LastUIOpen_Coroutine(eGUISetID _uiID)
	{
		yield return null;
		UI_Control.Instance.StartUI(_uiID);
	}

	public string StringFormat(string _body, string _arg1, string _arg2)
	{
		string rtn_txt = "";
		if (_body.Contains("{1}"))
			rtn_txt = string.Format(_body, _arg1, _arg2);
		else if (_body.Contains("{0}"))
			rtn_txt = string.Format(_body, _arg1);
		else
			rtn_txt = _body;

		return rtn_txt;
	}
}

