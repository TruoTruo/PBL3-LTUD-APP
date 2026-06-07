import csv
import json
import sys

def main(input_csv, output_json):
    with open(input_csv, 'r', encoding='utf-8') as f:
        reader = csv.reader(f)
        rows = list(reader)

    # Program info is in row 1 (index 1)
    prog_row = rows[1]
    
    prog_full_name = prog_row[1].strip()
    prog_id = ""
    prog_name = ""
    if "-" in prog_full_name:
        parts = prog_full_name.split("-", 1)
        prog_id = parts[0].strip()
        prog_name = parts[1].strip()
    else:
        prog_name = prog_full_name
        
    major = ""
    if "Công nghệ Thông tin" in prog_name:
        major = "Công nghệ Thông tin"
    elif "Trí tuệ nhân tạo" in prog_name:
        major = "Trí tuệ nhân tạo"
    else:
        major = prog_name
        
    duration = prog_row[2].strip() + " Học kỳ"
    total_credits = float(prog_row[3].strip() or 0)
    required_credits = float(prog_row[4].strip() or 0)
    elective_credits = float(prog_row[5].strip() or 0)

    prog_info = {
        "id": prog_id,
        "name": prog_name,
        "major": major,
        "total_credits": total_credits,
        "required_credits": required_credits,
        "elective_credits": elective_credits,
        "duration": duration
    }

    semesters_dict = {}
    
    current_course = None
    current_sem = None
    
    for i in range(4, len(rows)):
        row = rows[i]
        if not row or all(not cell.strip() for cell in row):
            continue
            
        tt = row[0].strip()
        if tt:
            if current_course and current_sem:
                if current_sem not in semesters_dict:
                    semesters_dict[current_sem] = []
                semesters_dict[current_sem].append(current_course)
                
            current_sem = int(row[1].strip() or 0)
            current_course = {
                "id": row[4].strip(),
                "name": row[2].strip(),
                "symbol": row[3].strip(),
                "credits": float(row[5].strip() or 0),
                "optional": row[6].strip(),
                "ht_da": row[7].strip(),
                "tq_da": row[8].strip(),
                "prerequisite": row[9].strip(),
                "corequisite": row[10].strip(),
                "relation": row[11].strip() if len(row) > 11 else ""
            }
        else:
            if current_course:
                if len(row) > 9 and row[9].strip():
                    if current_course["prerequisite"]:
                        current_course["prerequisite"] += ", " + row[9].strip()
                    else:
                        current_course["prerequisite"] = row[9].strip()
                
                if len(row) > 10 and row[10].strip():
                    if current_course["corequisite"]:
                        current_course["corequisite"] += ", " + row[10].strip()
                    else:
                        current_course["corequisite"] = row[10].strip()
                
                if len(row) > 11 and row[11].strip():
                    if current_course["relation"]:
                        current_course["relation"] += ", " + row[11].strip()
                    else:
                        current_course["relation"] = row[11].strip()

    if current_course and current_sem:
        if current_sem not in semesters_dict:
            semesters_dict[current_sem] = []
        semesters_dict[current_sem].append(current_course)
        
    semesters = []
    for sem in sorted(semesters_dict.keys()):
        semesters.append({
            "semester": sem,
            "courses": semesters_dict[sem]
        })
        
    output_data = {
        "program_info": prog_info,
        "semesters": semesters
    }

    with open(output_json, 'w', encoding='utf-8') as f:
        json.dump(output_data, f, ensure_ascii=False, indent=2)
        
    print(f"Successfully converted to {output_json}")

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python script.py <input.csv> <output.json>")
    else:
        main(sys.argv[1], sys.argv[2])
