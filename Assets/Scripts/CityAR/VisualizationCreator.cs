using System;
using System.Linq;
using DefaultNamespace;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.Utilities;
using TMPro;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace CityAR
{
    public class VisualizationCreator : MonoBehaviour
    {

        public GameObject districtPrefab;
        public GameObject housePrefab;
        public ToolTip tooltip;
        public TextMeshPro text;
        public PinchSlider slider;
        private DataObject _dataObject;
        private GameObject _platform;
        private Data _data;

        private int metricNum = 0;

        private void scaleHouse(GameObject gameObject,float s)
        {
            if (gameObject == null) return;
            var data = gameObject.GetComponent<TooltipData>();
            if (data != null)
            {
                var scale = gameObject.transform.localScale;
                var pos = gameObject.transform.localPosition;
                gameObject.transform.localScale = new Vector3(scale.x, data.value * s, scale.z);
                gameObject.transform.localPosition = new Vector3(pos.x, (data.value*s)/2f -0.49f, pos.z);
            }

            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                scaleHouse(gameObject.transform.GetChild(i).gameObject,s);
            }
            
        }

        public void changeScale(SliderEventData value)
        {
            scaleHouse(_platform, value.NewValue * 2f);
            try
            {
                _platform.GetComponent<BoundsControl>().UpdateBounds();
            }
            catch
            {
            }
        }

        public void setMetric(int met)
        {
            metricNum = met%4;
            slider.SliderValue = 0.5f;
            text.text = GetMetricString();
            _platform.GetComponent<BoundsControl>().enabled = false;
            Quaternion temp = _platform.transform.localRotation;
            for (int i = 0; i < _platform.transform.childCount; i++)
            {
                Destroy(_platform.transform.GetChild(i).gameObject);
            }
            _platform.transform.localRotation = Quaternion.Euler(0f,0f,0f);
            BuildCity(_data.ParseData());
            _platform.transform.localRotation = temp;
            _platform.GetComponent<BoundsControl>().enabled = true;
            _platform.GetComponent<BoundsControl>().UpdateBounds();
            
            
        }

        public float GetMetric(Entry entry)
        {
            var result = metricNum switch
            {
                0 =>entry.numberOfLines,
                1 =>entry.numberOfMethods,
                2 =>entry.numberOfAbstractClasses,
                3 =>entry.numberOfInterfaces,
                _ => entry.numberOfLines
            };
            return result;
        }
        
        private String GetMetricString()
        {
            var result = metricNum switch
            {
                0 =>"Number of Lines",
                1 =>"Number of Methods",
                2 =>"Number of Abstract Classes",
                3 =>"Number of Interfaces",
                _ => "Number of Lines"
            };
            return result;
        }

        private void Start()
        {
            _platform = gameObject;
            _data = _platform.GetComponent<Data>();
            _dataObject = _data.ParseData();
            BuildCity(_dataObject);
            
            _platform.GetComponent<BoundsControl>().UpdateBounds();
        }


        private void BuildCity(DataObject p)
        {
            if (p.project.files.Count <= 0) return;
            p.project.position = Vector3.up;
            p.project.scale = Vector3.one;
            p.project.depth = 1;
            BuildDistrict(p.project, _platform,false);
        }
        
        private void BuildDistrict(Entry entry, GameObject parent, bool splitHorizontal)
        {
            if (!entry.type.Equals("File"))
            {
                float dirLocs = GetMetric(entry);
                if (dirLocs == 0) return;
                entry.color = GetColorForDepth(entry.depth);
                parent = BuildDistrictBlock(entry, parent, false);

                var offset = 0f;
                var ratio = 0f;
                
                foreach (var subEntry in entry.files) {
                    if (subEntry.type.Equals("Dir"))
                    {
                        subEntry.depth = entry.depth + 1;
                        ratio = GetMetric(subEntry) / dirLocs;
                        offset += ratio*0.5f;
                        if (splitHorizontal)
                        {
                            subEntry.scale.Set(ratio,1f,1f);
                            subEntry.position.Set(RemapCoord(offset),1f,0f);
                        } else {
                            subEntry.scale.Set(1f,1f,ratio);
                            subEntry.position.Set(0f,1f,RemapCoord(offset));
                        }
                        offset += ratio* 0.5f;
                    }
                    BuildDistrict(subEntry, parent,!splitHorizontal);
                }
                
                ratio = 1f - offset;
                offset += ratio*0.5f;
                entry.scale = Vector3.one;
                entry.position = Vector3.up;
                if (ratio == 0) return;
                if (splitHorizontal)
                {
                    if (ContainsDirs(entry))
                    {
                        entry.scale.Set(ratio, 1f,1f);
                        entry.position.Set(RemapCoord(offset),1f,0f);
                    }
                    entry.depth += 1;
                    BuildDistrictBlock(entry, parent,true);
                }
                else
                {
                    if (ContainsDirs(entry))
                    {
                        entry.scale.Set(1f, 1f,ratio);
                        entry.position.Set(0f,1f,RemapCoord(offset));
                    }
                    entry.depth += 1;
                    BuildDistrictBlock(entry, parent,true);
                }
            }
        }
        
        private GameObject BuildDistrictBlock(Entry entry, GameObject parent, bool isBase)
        {
            if (entry == null || parent == null) return null;

            var prefabInstance = Instantiate(districtPrefab, parent.transform, true);

            if (!isBase)
            {
                prefabInstance.name = entry.name;
                prefabInstance.GetComponent<MeshRenderer>().material.color = entry.color;
                const float s = 0.005f;
                prefabInstance.transform.localScale =
                    entry.scale - ScaleToPlatform(parent.transform, new Vector3(s, 0f, s ));
                prefabInstance.transform.localPosition = entry.position;
            }
            else
            {
                prefabInstance.name = entry.name+"Base";
                prefabInstance.GetComponent<MeshRenderer>().enabled = false;
                float s = 0.005f;
                prefabInstance.transform.localScale =
                    entry.scale - ScaleToPlatform(parent.transform, new Vector3(s, 0f, s ));
                prefabInstance.transform.localPosition = entry.position;
           
                
                s = 0.01f;
                float filesCount = entry.files.Count;
                var unit = _platform.transform.GetComponent<Renderer>().bounds.size*s;
                var field = prefabInstance.GetComponent<Renderer>().bounds.size;
                var maxX = Mathf.Floor(field.x / (unit.x*2f));
                var maxZ = Mathf.Floor(field.z / (unit.z*2f));

                float gridSize = Mathf.Ceil(Mathf.Sqrt(filesCount));
                
                float boundX = gridSize;
                float boundZ = gridSize;
                float startX = 0.25f;
                float startZ = 0.25f;
                float stepsX = 1f / ((boundX+2)*2f);
                float stepsZ = 1f / ((boundZ+2)*2f);
                if (gridSize > maxX-1)
                {
                    boundX = maxX;
                    boundZ = gridSize;
                    stepsX = 1f / ((boundX+2));
                    stepsZ = 1f / ((boundZ+2)*2f);
                    startZ = 0.25f;
                    startX = 0.5f-stepsX;

                } else if (gridSize > maxZ-1)
                {
                    boundZ =  maxZ;
                    boundX = gridSize;
                    stepsX = 1f / ((boundX+2)*2f);
                    stepsZ = 1f / (boundZ+2);
                    startZ = 0.5f -stepsZ;
                    startX = 0.25f;
                }
                
                Vector3 offset = new Vector3(-startX, 0, -startZ);
                var hor = field.x > field.z;
                var curr = 0;
                for (int i = 0; i < filesCount; i++)
                {
                    var file = entry.files[i];
                    if (file.type.Equals("File"))
                    {
                        if (!hor)
                        {
                            if (curr >= boundX)
                            {
                                curr = 0;
                                offset.z += stepsZ;
                                offset.x = -startX;
                            }
                            curr++;
                            offset.x += stepsX;
                            }
                        else
                        {
                            if (curr >= boundZ)
                            {
                                curr = 0;
                                offset.x += stepsX;
                                offset.z = -startZ;
                            }
                            curr++;
                            offset.z += stepsZ;
                        }
                        file.position = offset;
                        BuildHouse(file,prefabInstance,s);
                    }
                }
                
            }
            

          
            return prefabInstance;
        }

        private void BuildHouse(Entry entry,GameObject parent, float s)
        {
            if (entry == null) return;
            var prefabInstance = Instantiate(housePrefab, parent.transform, true);
            prefabInstance.GetComponent<TouchScript>().setToolTop(tooltip);
            
            var data = prefabInstance.GetComponent<TooltipData>();
            data.metrik = GetMetricString();
            data.value = GetMetric(entry);
            data.fileName = entry.name;
            
            prefabInstance.name = entry.name;
            float height = GetMetric(entry);
            float h = height; /// 10f;
            var scale = ScaleToPlatform(parent.transform, new Vector3(s, s, s));
            prefabInstance.transform.localScale = new Vector3(scale.x, h, scale.z);
            prefabInstance.transform.localPosition = new Vector3(entry.position.x,h/2f -0.49f,entry.position.z);
        }

        private static bool ContainsDirs(Entry entry)
        {
            return entry.files.Any(e => e.type.Equals("Dir"));
        }

        private static float RemapCoord(float value)
        {
            return Remap(value,.0f,1.0f,-0.5f,0.5f);
        }

        private static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
        {
            return Mathf.Lerp(outMin, outMax, Mathf.InverseLerp(inMin, inMax, value));
        }

        private static Color GetColorForDepth(int depth)
        {
            var color = depth switch
            {
                1 => new Color(179f / 255f, 209f / 255f, 255f / 255f),
                2 => new Color(128f / 255f, 179f / 255f, 255f / 255f),
                3 => new Color(77f / 255f, 148f / 255f, 255f / 255f),
                4 => new Color(26f / 255f, 117f / 255f, 255f / 255f),
                5 => new Color(0f / 255f, 92f / 255f, 230f / 255f),
                _ => new Color(0f / 255f, 71f / 255f, 179f / 255f)
            };
            return color;
        }
        
        private Vector3 ScaleToPlatform(Transform parentTransform, Vector3 worldSize)
        {
            var transformedSize = new Vector3(worldSize.x, worldSize.y, worldSize.z);
            var t = parentTransform;
            do
            {
                transformedSize = transformedSize.Div(t.localScale);
                t = t.parent;
            }
            while (t != null && t != _platform.transform);
            return transformedSize;
        }
        
    }
}