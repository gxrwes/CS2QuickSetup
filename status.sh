#!/bin/bash

# Function to count video files and calculate folder size
count_files_and_size() {
    local folder="$1"
    local video_count=0
    local folder_size=0

    while IFS= read -rd '' file; do
        if [[ -f "$file" && "$file" =~ \.(mp4|mkv|avi|mov)$ ]]; then
            ((video_count++))
            folder_size=$((folder_size + $(stat -c%s "$file")))
        fi
    done < <(find "$folder" -type f -print0)

    echo "$folder | $video_count | $folder_size"
}

# Main script
output_file="history.csv"

if [[ -f "$output_file" ]]; then
    # Append to existing history.csv
    echo "folder | number of videofiles | size of folder" > temp.csv
    while IFS= read -r line; do
        count_files_and_size "$line" >> temp.csv
    done < <(cut -d '|' -f 1 "$output_file" | tail -n +2)
    cat temp.csv >> "$output_file"
    rm temp.csv
else
    # Create new history.csv
    echo "folder | number of videofiles | size of folder" > "$output_file"
fi

# Iterate through folders, count files, and calculate size
while IFS= read -r folder; do
    count_files_and_size "$folder" >> "$output_file"
done < <(find . -type d)
