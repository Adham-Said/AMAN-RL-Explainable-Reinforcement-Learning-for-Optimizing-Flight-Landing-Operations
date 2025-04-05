function val = get(a, propName)
% GET Get asset properties from the specified object
% and return the value
f=0;
l=length(a.attribs);
for i=1:l
    attrib=a.attribs(i);
    n=getname(attrib);
    if length(n)==length(propName)
        if getname(attrib)==propName
            f=1;
            val=getvalue(attrib);
        end
    end
end
if f==0
        error('DES properties: MAX_CLOCK')
end
    

