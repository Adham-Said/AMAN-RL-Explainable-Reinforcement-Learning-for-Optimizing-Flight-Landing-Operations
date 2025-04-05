function r=iseos(d)
if (d.numguestdelayed<d.delaylimit)
    r=0;
else
    r=1;
end