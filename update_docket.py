import re
import os

def update_docket(docket_path):
    if not os.path.exists(docket_path):
        print(f"Error: The file '{docket_path}' does not exist.")
        return

    with open(docket_path, 'r') as file:
        lines = file.readlines()

    status_section = False
    in_progress_section = False
    in_progress_tasks = []
    updated_lines = []

    for line in lines:
        if line.startswith("### STATUS"):
            status_section = True
            updated_lines.append(line)
            continue
        elif line.startswith("### IN PROGRESS"):
            status_section = False
            in_progress_section = True
            updated_lines.append(line)
            continue
        
        if in_progress_section:
            if line.startswith("### "):
                in_progress_section = False
                updated_lines.append(line)
            continue

        if status_section and re.search(r"\[\s\]", line):
            task_path = get_task_path(lines, line)
            if task_path:  # Ensure task_path is not empty
                in_progress_tasks.append(task_path)
        
        if not in_progress_section:
            updated_lines.append(line)
    
    # Write updated lines back to file
    with open(docket_path, 'w') as file:
        for line in updated_lines:
            file.write(line)
            if line.startswith("### IN PROGRESS"):
                if not in_progress_tasks:
                    file.write("- None\n")
                else:
                    for task in in_progress_tasks:
                        file.write(f"- {task}\n")
                        
    print("Updated 'IN PROGRESS' section:")
    for task in in_progress_tasks:
        print(f"- {task}")

def get_task_path(lines, task_line):
    task_index = lines.index(task_line)
    path = []
    for line in reversed(lines[:task_index]):
        if re.match(r"\s*\d+\.\s+\[\s\]\s+", line) or re.match(r"\s*\d+\.\s+\[\sx\]\s+", line):
            path.append(line.strip().split(' ', 2)[2].split('**')[1])
        elif re.match(r"(\s+\[\s\]\s+|\s+\[\sx\]\s+)\*\*", line):
            path.append(line.strip().split('**')[1])
    return '.'.join(reversed(path))

# Specify the path to your Docket.md file
docket_path = os.path.join(os.getcwd(), 'Docket.md')
update_docket(docket_path)
