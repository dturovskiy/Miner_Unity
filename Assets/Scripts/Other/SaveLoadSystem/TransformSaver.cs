using LitJson;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformSaver : SaveableBehavior
{
    private const string LOCAL_POSITION_KEY = "localPosition";
    private const string IS_ACTIVE_KEY = "isActive";
    private const string IS_DESTROYED_KEY = "isDestroyed";

    private JsonData SerializeValue(object obj)
    {
        return JsonMapper.ToObject(JsonUtility.ToJson(obj));
    }

    private T DesearializeValue<T>(JsonData data)
    {
        return JsonUtility.FromJson<T>(data.ToJson());
    }

    public override JsonData SavedData
    {
        get 
        { 
            var result = new JsonData();

            result[LOCAL_POSITION_KEY] = SerializeValue(transform.localPosition);
            result[IS_ACTIVE_KEY] = gameObject.activeSelf;
            result[IS_DESTROYED_KEY] = !gameObject.activeInHierarchy;

            return result;
        }
    }

    public override void LoadFromData(JsonData data)
    {
        if(data.ContainsKey(LOCAL_POSITION_KEY))
        {
            transform.localPosition = DesearializeValue<Vector3>(data[LOCAL_POSITION_KEY]);
        }

        if (data.ContainsKey(IS_ACTIVE_KEY))
        {
            gameObject.SetActive((bool)data[IS_ACTIVE_KEY]);
        }

        if(data.ContainsKey(IS_DESTROYED_KEY) && (bool)data[IS_DESTROYED_KEY])
        {
            Destroy(gameObject);
        }
    }
}
