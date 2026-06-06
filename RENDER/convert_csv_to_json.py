import csv
import json

def convert_csv_to_json(csv_filepath, json_filepath):
    classes_dict = {}

    with open(csv_filepath, mode='r', encoding='utf-8') as file:
        reader = csv.DictReader(file)
        
        # In case there are duplicate column names (like 'Mã GV'), DictReader handles it 
        # by making the second one accessible if we know how it's keyed, 
        # but let's just use the fieldnames directly or rely on DictReader.
        
        for row in reader:
            # Handle potential newlines in fields due to quoted CSV values
            for k in row:
                if row[k] is not None:
                    row[k] = row[k].strip()

            stt = row.get('STT')
            khoa = row.get('Khoá')
            nhom = row.get('Nhóm')
            ma_lop_hp = row.get('Mã lớp HP')
            ten_mon = row.get('tenmon')
            
            # Mã GV might be tricky since it appears twice. 
            # We can use the first one if we can extract it.
            ma_gv = row.get('Mã GV')
            
            tuan = row.get('Tuần')
            khoa_day = row.get('khoa dạy')
            malop = row.get('malop')
            ten_gv = row.get('Tên giảng viên')
            
            thu2 = row.get('Thứ 2', '')
            thu3 = row.get('Thứ 3', '')
            thu4 = row.get('Thứ 4', '')
            thu5 = row.get('Thứ 5', '')
            thu6 = row.get('Thứ 6', '')
            thu7 = row.get('Thứ 7', '')
            cn = row.get('CN', '')

            if not malop:
                continue

            if malop not in classes_dict:
                classes_dict[malop] = {
                    "class_name": malop,
                    "khoa": khoa,
                    "courses": []
                }

            course = {
                "stt": stt,
                "id": ma_lop_hp,
                "name": ten_mon,
                "group": nhom,
                "lecturer_id": ma_gv,
                "lecturer_name": ten_gv,
                "weeks": tuan,
                "department": khoa_day,
                "thu2": thu2,
                "thu3": thu3,
                "thu4": thu4,
                "thu5": thu5,
                "thu6": thu6,
                "thu7": thu7,
                "cn": cn
            }
            
            classes_dict[malop]["courses"].append(course)

    # Sort classes by name just to be neat
    sorted_classes = sorted(list(classes_dict.values()), key=lambda x: x["class_name"])

    output_data = {
        "schedule_info": {
            "semester": "HK2_2025",
            "total_classes": len(sorted_classes)
        },
        "classes": sorted_classes
    }

    with open(json_filepath, mode='w', encoding='utf-8') as json_file:
        json.dump(output_data, json_file, ensure_ascii=False, indent=2)
        
    print(f"Successfully converted {csv_filepath} to {json_filepath}")

if __name__ == "__main__":
    import sys
    csv_file = "d:/IT/HỌC/PBL3/RENDER/HK2_2025.csv"
    json_file = "d:/IT/HỌC/PBL3/PBL3-LTUD-APP/RENDER/HK2_2025.json"
    convert_csv_to_json(csv_file, json_file)
