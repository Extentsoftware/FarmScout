#!/usr/bin/env python3
"""
Example usage of the enhanced moisture_report.py script
Demonstrates how to generate markdown reports with embedded graphics
"""

from moisture_report import create_moisture_report

def main():
    """Demonstrate different output formats"""
    
    print("=== Moisture Report Generation Examples ===\n")
    
    # Example 1: Generate PNG only (original behavior)
    print("1. Generating PNG report only...")
    create_moisture_report('png')
    print("   → Creates: soil_moisture_report.png\n")
    
    # Example 2: Generate markdown with embedded graphics
    print("2. Generating markdown report with embedded graphics...")
    create_moisture_report('markdown')
    print("   → Creates: soil_moisture_report.md (with embedded base64 image)\n")
    
    # Example 3: Generate both formats
    print("3. Generating both PNG and markdown...")
    create_moisture_report('all')
    print("   → Creates: soil_moisture_report.png")
    print("   → Creates: soil_moisture_report.md")
    print("   → Creates: soil_moisture_report.txt\n")
    
    print("=== Report Files Generated ===")
    print("• soil_moisture_report.png - High-resolution dashboard image")
    print("• soil_moisture_report.md  - Markdown report with embedded graphics")
    print("• soil_moisture_report.txt - Plain text report")
    
    print("\n=== Markdown Features ===")
    print("• Embedded base64-encoded graphics")
    print("• Structured sections with emojis")
    print("• Tables for rankings and statistics")
    print("• Key insights and recommendations")
    print("• Professional formatting")

if __name__ == "__main__":
    main() 