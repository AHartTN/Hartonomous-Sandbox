import onnx
import json
import sys

def main():
    if len(sys.argv) < 2:
        print("Usage: python parse_onnx.py <model_path>")
        return

    model_path = sys.argv[1]
    model = onnx.load(model_path)

    output = {
        "name": model.graph.name,
        "type": "ONNX",
        "architecture": model.domain,
        "layers": []
    }

    for node in model.graph.node:
        layer = {
            "name": node.name,
            "type": node.op_type,
            "parameters": {},
            "weights": None
        }
        output["layers"].append(layer)

    print(json.dumps(output, indent=4))

if __name__ == "__main__":
    main()
