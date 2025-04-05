function e = event(t, c, varargin)
    % Create attribute for Type and add it
    attrib = attribute('Type', t);
    e.attribs(1) = attrib;
    % Create an attribute for ServerIndex if provided
    if ~isempty(varargin)
        serverIdx = varargin{1};
        attrib2 = attribute('ServerIndex', serverIdx);
        e.attribs(end+1) = attrib2;
    end
    e = class(e, 'event');
    e = set(e, 'Time', c);
end
