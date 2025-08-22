import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns
from datetime import datetime
import numpy as np

def parse_date(date_str):
    """Parse date string to datetime object"""
    try:
        return datetime.strptime(date_str, '%d/%m/%Y')
    except:
        return None

def condition_to_numeric(condition):
    """Convert condition to numeric value for analysis"""
    condition_map = {
        'pass': 3,
        'partial': 2,
        'fail': 1,
        'n/a': 0
    }
    return condition_map.get(condition.lower(), 0)

def create_section_summary_dashboard():
    """Create dashboard showing section health summary from last 200 lines"""
    
    # Read the CSV file
    df = pd.read_csv('scout.csv')
    
    # Rename columns to match expected names
    df = df.rename(columns={
        'Date': 'date',
        'Section': 'section', 
        'Observation Type': 'metric',
        'Pass/Fail': 'condition',
        'Scout': 'scout',
        'Notes': 'notes'
    })
    
    # Get last 200 lines
    df_last_200 = df.tail(200).copy()
    
    # Parse dates
    df_last_200['date'] = df_last_200['date'].apply(parse_date)
    df_last_200 = df_last_200.dropna(subset=['date'])
    
    # Convert conditions to numeric values
    df_last_200['condition_numeric'] = df_last_200['condition'].apply(condition_to_numeric)
    
    # Get the most recent data for each section from the last 200 lines
    latest_data = df_last_200.loc[df_last_200.groupby('section')['date'].idxmax()]
    
    # Create the dashboard
    fig = plt.figure(figsize=(20, 16))
    
    # Set up the grid
    gs = fig.add_gridspec(4, 3, hspace=0.4, wspace=0.3)
    
    # 1. Section Health Summary (Bar Chart)
    ax1 = fig.add_subplot(gs[0, :2])
    
    # Calculate average health score per section
    section_scores = latest_data.groupby('section')['condition_numeric'].mean().sort_values(ascending=True)
    
    colors = ['red' if score < 1.5 else 'orange' if score < 2.5 else 'green' for score in section_scores.values]
    bars = ax1.barh(range(len(section_scores)), section_scores.values, color=colors)
    ax1.set_yticks(range(len(section_scores)))
    ax1.set_yticklabels(section_scores.index)
    ax1.set_xlabel('Health Score (1=Fail, 2=Partial, 3=Pass)')
    ax1.set_title('Section Health Summary (Last 200 Lines)', fontsize=14, fontweight='bold')
    ax1.set_xlim(0, 3)
    ax1.grid(True, alpha=0.3)
    
    # Add value labels on bars
    for i, bar in enumerate(bars):
        width = bar.get_width()
        ax1.text(width + 0.05, bar.get_y() + bar.get_height()/2, 
                f'{width:.1f}', ha='left', va='center', fontweight='bold')
    
    # 2. Metric Performance Analysis
    ax2 = fig.add_subplot(gs[0, 2])
    
    metric_performance = latest_data.groupby('metric')['condition_numeric'].mean().sort_values(ascending=True)
    
    bars = ax2.barh(range(len(metric_performance)), metric_performance.values, color='lightblue')
    ax2.set_yticks(range(len(metric_performance)))
    ax2.set_yticklabels(metric_performance.index, fontsize=9)
    ax2.set_xlabel('Average Score')
    ax2.set_title('Metric Performance\n(Last 200 Lines)', fontsize=12, fontweight='bold')
    ax2.set_xlim(0, 3)
    
    # 3. Condition Distribution
    ax3 = fig.add_subplot(gs[1, 0])
    
    condition_counts = latest_data['condition'].value_counts()
    colors = ['red', 'orange', 'green']
    wedges, texts, autotexts = ax3.pie(condition_counts.values, labels=condition_counts.index, 
                                       autopct='%1.1f%%', colors=colors, startangle=90)
    ax3.set_title('Condition Distribution\n(Last 200 Lines)', fontsize=12, fontweight='bold')
    
    # 4. Section Activity (Number of observations per section)
    ax4 = fig.add_subplot(gs[1, 1])
    
    section_activity = df_last_200.groupby('section').size().sort_values(ascending=True)
    
    bars = ax4.barh(range(len(section_activity)), section_activity.values, color='lightcoral')
    ax4.set_yticks(range(len(section_activity)))
    ax4.set_yticklabels(section_activity.index, fontsize=8)
    ax4.set_xlabel('Number of Observations')
    ax4.set_title('Section Activity\n(Last 200 Lines)', fontsize=12, fontweight='bold')
    
    # 5. Recent Health Trends (Last 200 lines timeline)
    ax5 = fig.add_subplot(gs[1, 2])
    
    # Calculate daily average health score
    daily_health = df_last_200.groupby('date')['condition_numeric'].mean()
    
    ax5.plot(daily_health.index, daily_health.values, marker='o', linewidth=2, markersize=4)
    ax5.set_title('Recent Health Trend\n(Last 200 Lines)', fontsize=12, fontweight='bold')
    ax5.set_xlabel('Date')
    ax5.set_ylabel('Average Health Score')
    ax5.grid(True, alpha=0.3)
    ax5.set_ylim(0, 3)
    plt.setp(ax5.xaxis.get_majorticklabels(), rotation=45)
    
    # 6. Warning Notes and Critical Issues
    ax6 = fig.add_subplot(gs[2, :])
    ax6.axis('off')
    
    # Find all issues with notes
    issues_with_notes = latest_data[latest_data['notes'].notna() & (latest_data['notes'] != '')]
    
    warning_text = "WARNING NOTES AND CRITICAL ISSUES:\n\n"
    
    if not issues_with_notes.empty:
        for _, row in issues_with_notes.iterrows():
            status_icon = "🔴" if row['condition'].lower() == 'fail' else "🟡" if row['condition'].lower() == 'partial' else "🟢"
            warning_text += f"{status_icon} {row['section']} - {row['metric']}: {row['condition']}\n"
            warning_text += f"   Note: {row['notes']}\n\n"
    else:
        warning_text += "✅ No warning notes found in recent data.\n"
    
    ax6.text(0.05, 0.95, warning_text, transform=ax6.transAxes, fontsize=10,
             verticalalignment='top', bbox=dict(boxstyle='round', facecolor='lightyellow', alpha=0.8))
    
    # 7. Detailed Section Analysis
    ax7 = fig.add_subplot(gs[3, :])
    ax7.axis('off')
    
    # Create detailed analysis for each section
    analysis_text = "DETAILED SECTION ANALYSIS:\n\n"
    
    for section in sorted(latest_data['section'].unique()):
        section_data = latest_data[latest_data['section'] == section]
        avg_score = section_data['condition_numeric'].mean()
        
        status = "🟢 GOOD" if avg_score >= 2.5 else "🟡 FAIR" if avg_score >= 1.5 else "🔴 POOR"
        analysis_text += f"{section}: {status} (Score: {avg_score:.2f})\n"
        
        # List metrics for this section
        for _, row in section_data.iterrows():
            metric_status = "🔴" if row['condition'].lower() == 'fail' else "🟡" if row['condition'].lower() == 'partial' else "🟢"
            analysis_text += f"  {metric_status} {row['metric']}: {row['condition']}\n"
        
        analysis_text += "\n"
    
    ax7.text(0.05, 0.95, analysis_text, transform=ax7.transAxes, fontsize=9,
             verticalalignment='top', bbox=dict(boxstyle='round', facecolor='lightblue', alpha=0.8))
    
    # Save the dashboard
    plt.savefig('section_summary_dashboard.png', dpi=300, bbox_inches='tight')
    print("Section Summary Dashboard saved as 'section_summary_dashboard.png'")
    
    # Generate text report
    generate_summary_text_report(df_last_200, latest_data)
    
    plt.show()

def generate_summary_text_report(df_last_200, latest_data):
    """Generate a detailed text report for the last 200 lines"""
    
    report = []
    report.append("=" * 80)
    report.append("SECTION HEALTH SUMMARY REPORT (Last 200 Lines)")
    report.append("=" * 80)
    report.append(f"Report Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    report.append(f"Data Range: {df_last_200['date'].min().strftime('%Y-%m-%d')} to {df_last_200['date'].max().strftime('%Y-%m-%d')}")
    report.append(f"Total Observations Analyzed: {len(df_last_200)}")
    report.append("")
    
    # Summary Statistics
    report.append("SUMMARY STATISTICS:")
    report.append("-" * 40)
    report.append(f"Total Sections Monitored: {latest_data['section'].nunique()}")
    report.append(f"Total Metrics Tracked: {latest_data['metric'].nunique()}")
    report.append(f"Date Range: {df_last_200['date'].min().strftime('%Y-%m-%d')} to {df_last_200['date'].max().strftime('%Y-%m-%d')}")
    report.append("")
    
    # Section Health Rankings
    report.append("SECTION HEALTH RANKINGS:")
    report.append("-" * 40)
    section_scores = latest_data.groupby('section')['condition_numeric'].mean().sort_values(ascending=False)
    for i, (section, score) in enumerate(section_scores.items(), 1):
        status = "🟢 GOOD" if score >= 2.5 else "🟡 FAIR" if score >= 1.5 else "🔴 POOR"
        report.append(f"{i:2d}. {section}: {score:.2f} {status}")
    report.append("")
    
    # Metric Performance
    report.append("METRIC PERFORMANCE:")
    report.append("-" * 40)
    metric_scores = latest_data.groupby('metric')['condition_numeric'].mean().sort_values(ascending=False)
    for metric, score in metric_scores.items():
        status = "🟢 GOOD" if score >= 2.5 else "🟡 FAIR" if score >= 1.5 else "🔴 POOR"
        report.append(f"• {metric}: {score:.2f} {status}")
    report.append("")
    
    # Warning Notes and Critical Issues
    report.append("WARNING NOTES AND CRITICAL ISSUES:")
    report.append("-" * 40)
    issues_with_notes = latest_data[latest_data['notes'].notna() & (latest_data['notes'] != '')]
    if not issues_with_notes.empty:
        for _, row in issues_with_notes.iterrows():
            status_icon = "🔴" if row['condition'].lower() == 'fail' else "🟡" if row['condition'].lower() == 'partial' else "🟢"
            report.append(f"{status_icon} {row['section']} - {row['metric']}: {row['condition']}")
            report.append(f"   Note: {row['notes']}")
            report.append("")
    else:
        report.append("✅ No warning notes found in recent data.")
    report.append("")
    
    # Detailed Section Analysis
    report.append("DETAILED SECTION ANALYSIS:")
    report.append("-" * 40)
    for section in sorted(latest_data['section'].unique()):
        section_data = latest_data[latest_data['section'] == section]
        avg_score = section_data['condition_numeric'].mean()
        
        status = "🟢 GOOD" if avg_score >= 2.5 else "🟡 FAIR" if avg_score >= 1.5 else "🔴 POOR"
        report.append(f"\n{section}: {status} (Score: {avg_score:.2f})")
        
        # List metrics for this section
        for _, row in section_data.iterrows():
            metric_status = "🔴" if row['condition'].lower() == 'fail' else "🟡" if row['condition'].lower() == 'partial' else "🟢"
            report.append(f"  {metric_status} {row['metric']}: {row['condition']}")
            if pd.notna(row['notes']) and str(row['notes']).strip():
                report.append(f"    Note: {row['notes']}")
    
    report.append("")
    report.append("=" * 80)
    
    # Save text report
    with open('section_summary_report.txt', 'w', encoding='utf-8') as f:
        f.write('\n'.join(report))
    
    # Print report to console
    print('\n'.join(report))
    print("\nText report saved as 'section_summary_report.txt'")

if __name__ == "__main__":
    create_section_summary_dashboard() 