function a = set(a,varargin)
 propertyArgIn = varargin;
 while length(propertyArgIn) >= 2,
    prop = propertyArgIn{1};
    val = propertyArgIn{2};
    propertyArgIn = propertyArgIn(3:end);
    f=0;
    l=length(a.attribs);
    for i=1:l
        attrib=a.attribs(i);
        n=getname(attrib);
        if length(n)==length(prop)
            if n==prop
            f=1;
            attrib=setvalue(attrib,val);            
            a.attribs(i)=attrib;
            end
        end
    end
    if f==0
        attrib=attribute(prop,val);
        a.attribs(l+1)=attrib;
    end
 end