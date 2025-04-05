function val = get(a, propName)
    switch (propName)
        case 'avgs'
            val = a.avgs;  % access testtSim-specific field
        case 'CLOCK'
            val = get(a.des, 'CLOCK');  % Access CLOCK from the embedded des object
        otherwise
            val = get(a.des, propName);  % Delegate to the des object
    end
end
