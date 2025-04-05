function [utilization, queueLength, delay] = extractLastMetrics(filename)
    % Default values in case of an error or missing data
    utilization = 0;
    queueLength = 0;
    delay = 0;
    
    fid = fopen(filename, 'r');
    if fid == -1
        error('Could not open log file.');
    end

    while ~feof(fid)
        line = fgetl(fid);
        
        if contains(line, 'Average Server Utilization =')
            tokens = regexp(line, '[-+]?[0-9]*\.?[0-9]+', 'match');
            utilization = str2double(tokens{end});
        elseif contains(line, 'Average queue length =')
            tokens = regexp(line, '[-+]?[0-9]*\.?[0-9]+', 'match');
            queueLength = str2double(tokens{end});
        elseif contains(line, 'Average Delay =')
            tokens = regexp(line, '[-+]?[0-9]*\.?[0-9]+', 'match');
            delay = str2double(tokens{end});
        end
    end

    fclose(fid);
end
