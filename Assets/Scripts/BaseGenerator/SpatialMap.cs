using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpatialMap
{
    readonly bool[,] cells; // [x,z]
    public SpatialMap(int sizeX,int sizeZ) => cells = new bool[sizeX,sizeZ];

    public bool IsFree(int x,int z)=> !cells[x,z];
    public void Mark (int x,int z)=>  cells[x,z]=true;

    public bool Fits(bool[,] mask,int gx,int gz){
        int w=mask.GetLength(0), h=mask.GetLength(1);
        for(int ix=0;ix<w;++ix)
        for(int iz=0;iz<h;++iz)
            if(mask[ix,iz] && !IsFree(gx+ix,gz+iz)) return false;
        return true;
    }
    public void Stamp(bool[,] mask,int gx,int gz){
        int w=mask.GetLength(0), h=mask.GetLength(1);
        for(int ix=0;ix<w;++ix)
        for(int iz=0;iz<h;++iz)
            if(mask[ix,iz]) Mark(gx+ix,gz+iz);
    }
}
