import os
import re

directory = r"d:\IT\HỌC\PBL3\PBL3-LTUD-APP\StudentReminderApp"

color_map = {
    "{DynamicResource BgBrush}": ["#F8FAFC", "#F0F2F5", "#F8F9FA", "#F9FAFB"],
    "{DynamicResource SurfaceBrush}": ["#FFFFFF", "White"],
    "{DynamicResource TextPrimaryBrush}": ["#111827", "#0F172A", "#1E293B", "#111111", "#1A1A1A", "#333333", "#374151"],
    "{DynamicResource TextSecondaryBrush}": ["#64748B", "#6B7280", "#475569", "#555"],
    "{DynamicResource TextMutedBrush}": ["#94A3B8", "#9CA3AF", "#A1A1AA", "#888"],
    "{DynamicResource BorderBrush}": ["#E2E8F0", "#E5E7EB", "#F3F4F6", "#DDD", "#E5E5E5"]
}

for root, dirs, files in os.walk(directory):
    if r"\obj" in root or r"\bin" in root or r"\Themes" in root:
        continue
    for file in files:
        if file.endswith(".xaml"):
            filepath = os.path.join(root, file)
            with open(filepath, "r", encoding="utf-8") as f:
                content = f.read()
            
            original_content = content
            
            # Replace StaticResource with DynamicResource for Brushes
            content = re.sub(r'\{StaticResource (\w+Brush)\}', r'{DynamicResource \1}', content)
            
            # Replace Hardcoded Colors
            for replacement, colors in color_map.items():
                for color in colors:
                    pattern = r'(?i)(\b(?:Background|Foreground|BorderBrush|Fill|Stroke|Value)=)"' + color + r'"'
                    content = re.sub(pattern, rf'\1"{replacement}"', content)
            
            if content != original_content:
                with open(filepath, "w", encoding="utf-8") as f:
                    f.write(content)
                print("Updated a file")

print("Done replacing colors.")
