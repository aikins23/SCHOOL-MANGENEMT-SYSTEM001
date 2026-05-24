from PIL import Image, ImageDraw
import os

# Create a 1200x800 background image
width, height = 1200, 800
img = Image.new('RGB', (width, height), color=(135, 206, 235))

# Add a gradient effect manually
draw = ImageDraw.Draw(img, 'RGBA')

# Draw gradient from light blue to lighter blue
for y in range(height):
    ratio = y / height
    r = int(135 + (200 - 135) * ratio)
    g = int(206 + (230 - 206) * ratio)
    b = int(235 + (255 - 235) * ratio)
    draw.line([(0, y), (width, y)], fill=(r, g, b))

# Add subtle decorative elements
for x in range(0, width, 150):
    draw.line([(x, 0), (x, 50)], fill=(100, 149, 237, 128), width=2)

# Save the image
output_path = r'C:\Users\DELL\Downloads\New folder (2)\IPMC PROJECT BUABENG EMMANUEL AIKINS (1)\BUABENG EMMANUEL AIKINS - Copy\Resources\school_background.png'
img.save(output_path)
print(f"Background image created: {output_path}")
