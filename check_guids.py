import json
import re
from collections import Counter

def extract_guids_from_json(file_path):
    """Extract all GUIDs from the JSON file and check for duplicates."""
    try:
        with open(file_path, 'r', encoding='utf-8') as file:
            data = json.load(file)
        
        # Function to recursively extract IDs from nested structures
        def extract_ids(obj, ids=None):
            if ids is None:
                ids = []
            
            if isinstance(obj, dict):
                if 'id' in obj:
                    ids.append(obj['id'])
                for value in obj.values():
                    extract_ids(value, ids)
            elif isinstance(obj, list):
                for item in obj:
                    extract_ids(item, ids)
            
            return ids
        
        # Extract all IDs
        all_ids = extract_ids(data)
        
        # Check for duplicates
        id_counts = Counter(all_ids)
        duplicates = {id_val: count for id_val, count in id_counts.items() if count > 1}
        
        # Print results
        print(f"Total GUIDs found: {len(all_ids)}")
        print(f"Unique GUIDs: {len(set(all_ids))}")
        
        if duplicates:
            print(f"\n❌ DUPLICATE GUIDs found ({len(duplicates)}):")
            for id_val, count in duplicates.items():
                print(f"  {id_val} appears {count} times")
        else:
            print("\n✅ All GUIDs are unique!")
        
        # Print all GUIDs for verification
        print(f"\nAll GUIDs in the file:")
        for i, guid in enumerate(sorted(all_ids), 1):
            print(f"  {i:3d}. {guid}")
        
        return all_ids, duplicates
        
    except FileNotFoundError:
        print(f"Error: File {file_path} not found")
        return [], {}
    except json.JSONDecodeError as e:
        print(f"Error: Invalid JSON in {file_path}: {e}")
        return [], {}
    except Exception as e:
        print(f"Error: {e}")
        return [], {}

if __name__ == "__main__":
    # Check the lookup_data_seeding.json file
    file_path = "FarmScout/Resources/Raw/lookup_data_seeding.json"
    guids, duplicates = extract_guids_from_json(file_path)
    
    if duplicates:
        print(f"\n❌ SUMMARY: Found {len(duplicates)} duplicate GUIDs")
        exit(1)
    else:
        print(f"\n✅ SUMMARY: All {len(guids)} GUIDs are unique")
        exit(0)
