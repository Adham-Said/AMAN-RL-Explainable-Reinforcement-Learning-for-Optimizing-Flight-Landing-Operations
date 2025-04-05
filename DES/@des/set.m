function a = set(a,varargin)
propertyArgIn = varargin;
while length(propertyArgIn) >= 2,
    prop = propertyArgIn{1};
    val = propertyArgIn{2};
    propertyArgIn = propertyArgIn(3:end);
    switch prop
        case 'MAX_CLOCK'
            a.MAX_CLOCK = val;
        otherwise
            error('DES properties: MAX_CLOCK')
    end
end