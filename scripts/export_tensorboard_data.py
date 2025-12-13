import argparse
from pathlib import Path

from tbparse import SummaryReader


def export_tensorboard_to_csv(
    logdir: str = "./logs",
    tags: list[str] | None = None,
):
    logdir = Path(logdir)

    if not logdir.exists():
        print(f"Error: Log directory not found: {logdir}")
        return

    print(f"Reading TensorBoard logs from: {logdir}")
    reader = SummaryReader(logdir, pivot=True)

    scalars_df = reader.scalars
    if scalars_df.empty:
        print("No scalar data found in logs.")
        return

    print(f"Found {len(scalars_df)} rows, {len(scalars_df.columns)} columns")
    print(f"Available tags: {list(scalars_df.columns)}")

    if tags:
        available_cols = ["step"] + [t for t in tags if t in scalars_df.columns]
        missing = [t for t in tags if t not in scalars_df.columns]
        if missing:
            print(f"Warning: Tags not found: {missing}")
        scalars_df = scalars_df[available_cols]

    output_path = Path(f"./{logdir.name}.csv")
    scalars_df.to_csv(output_path, index=False)
    print(f"Exported to: {output_path}")

    print(f"\nExported columns:")
    for col in scalars_df.columns:
        print(f"  - {col}")


def main():
    parser = argparse.ArgumentParser(description="Export TensorBoard data to CSV")
    parser.add_argument(
        "--logdir",
        type=str,
        default="./logs",
        help="TensorBoard log directory (default: ./logs)",
    )
    parser.add_argument(
        "--tags",
        type=str,
        nargs="*",
        help="Specific tags to export (default: all)",
    )

    args = parser.parse_args()
    export_tensorboard_to_csv(
        logdir=args.logdir,
        tags=args.tags,
    )


if __name__ == "__main__":
    main()
