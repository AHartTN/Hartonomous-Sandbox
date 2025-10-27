import tensorflow as tf
import os

# 1. Define a simple Keras model
def create_simple_model():
    model = tf.keras.Sequential([
        tf.keras.layers.Dense(10, activation='relu', input_shape=(5,)),
        tf.keras.layers.Dense(1, activation='sigmoid')
    ])
    return model

# Create the model
model = create_simple_model()

# Compile the model (optional for saving, but good practice if you intend to train)
model.compile(optimizer='adam', loss='binary_crossentropy', metrics=['accuracy'])

# 2. Create some dummy data for training (optional, but makes the model more "complete")
import numpy as np
x_train = np.random.rand(100, 5).astype(np.float32)
y_train = np.random.randint(0, 2, (100, 1)).astype(np.float32)

# Train the model (optional)
print("Training the model...")
model.fit(x_train, y_train, epochs=5, verbose=0)
print("Model training complete.")

# 3. Define the directory to save the model
model_save_path = "./simple_tf_model"

# Save the model in the SavedModel format
# This will create a directory named 'simple_tf_model' containing 'saved_model.pb'
print(f"Saving the model to: {model_save_path}")
tf.saved_model.save(model, model_save_path)
print("Model saved successfully.")

# 4. Verify the saved model (optional)
print(f"Contents of the saved model directory '{model_save_path}':")
for root, dirs, files in os.walk(model_save_path):
    level = root.replace(model_save_path, '').count(os.sep)
    indent = ' ' * 4 * (level)
    print(f'{indent}{os.path.basename(root)}/')
    subindent = ' ' * 4 * (level + 1)
    for f in files:
        print(f'{subindent}{f}')

# 5. Load the model back (demonstration)
print(f"\nLoading the model from: {model_save_path}")
loaded_model = tf.saved_model.load(model_save_path)
print("Model loaded successfully.")

# You can now use the loaded model for inference
# For example, if you want to make predictions:
 dummy_input = np.random.rand(1, 5).astype(np.float32)
print(f"Dummy input for prediction: {dummy_input}")
# The loaded model might expose its call function directly or through a signature
# For a simple Keras model saved this way, you can often call it directly
predictions = loaded_model(dummy_input)
print(f"Predictions from loaded model: {predictions.numpy()}")
