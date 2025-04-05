function val = get(a, propName)
switch propName
    case 'MAX_CLOCK'
        val = a.MAX_CLOCK;
    case 'CLOCK'
        val = a.clock;
    case 'S'
        val = a.s;  % returns the recorded server states
    case 'AVGS'
        val = a.avgs;  % returns the recorded server states
    otherwise
        error('DES properties: Unknown property %s', propName)
end
