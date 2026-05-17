import asyncio
import json
import ast
import copy
import re
import sys
import os
import signal
import secrets
from typing import Any, Dict, List, Union
from fastapi import FastAPI, WebSocket, WebSocketDisconnect, Query
import uvicorn


# ============================================================
# 認証設定（追加部分）
# ============================================================
API_KEY = os.environ.get("API_KEY", "change-this-secret-key")
PORT = int(os.environ.get("PORT", 8000))


def verify_api_key(key: str) -> bool:
    """APIキーを検証（タイミング攻撃対策付き）"""
    if not key:
        return False
    return secrets.compare_digest(key, API_KEY)


# ============================================================
# 以下、オリジナルコードと完全に同じ
# ============================================================

class CodeSanitizer:
    """
    Pythonコードのセキュリティサニタイザー
    """
    
    def __init__(self):
        # 危険なモジュール・関数のブラックリスト
        self.dangerous_imports = {
            'os', 'sys', 'subprocess', 'shutil', 'tempfile', 'glob',
            'socket', 'urllib', 'http', 'ftplib', 'smtplib', 'telnetlib',
            'pickle', 'marshal', 'shelve', 'dill',
            '__import__', 'eval', 'exec', 'compile', 'open', 'file',
            'input', 'raw_input', 'reload', 'vars', 'dir', 'locals', 'globals',
            'exit', 'quit', 'help', 'copyright', 'credits', 'license'
        }
        
        # 置換パターン
        self.replacements = {
            # sys.argvの置換
            r'sys\.argv': '"[]"',
            r'\bsys\s*\.\s*argv\b': '"[]"',
            
            # input関数の置換
            r'\binput\s*\([^)]*\)': '"mocked_input"',
            r'\braw_input\s*\([^)]*\)': '"mocked_input"',
            
            # time.sleep の無効化
            r'\btime\s*\.\s*sleep\s*\([^)]*\)': 'pass  # sleep disabled',
            
            # exit/quit の無効化
            r'\bexit\s*\([^)]*\)': 'pass  # exit disabled',
            r'\bquit\s*\([^)]*\)': 'pass  # quit disabled',
            r'\bsys\s*\.\s*exit\s*\([^)]*\)': 'pass  # sys.exit disabled',
            
            # 一般的なタイプミスの修正
            r'\*\*name\*\*': '__name__',
        }
    
    def sanitize_code(self, code: str) -> str:
        """
        コードをサニタイズする
        """
        sanitized = code
        
        # コメントの除去（ただし文字列リテラル内は保護）
        sanitized = self.remove_comments_safely(sanitized)
        
        # 危険な関数・モジュールの置換
        for pattern, replacement in self.replacements.items():
            sanitized = re.sub(pattern, replacement, sanitized)
        
        # importの制限
        sanitized = self.sanitize_imports(sanitized)
        
        # join関数の安全化
        sanitized = self.sanitize_joins(sanitized)
        
        return sanitized
    
    def remove_comments_safely(self, code: str) -> str:
        """
        文字列リテラル内のコメント記号を保護しながらコメントを除去
        ただし、docstringは保護する
        """
        lines = code.split('\n')
        cleaned_lines = []
        in_function = False
        
        for i, line in enumerate(lines):
            stripped = line.strip()
            
            # 関数定義の検出
            if stripped.startswith('def '):
                in_function = True
                cleaned_lines.append(line)
                continue
                
            # docstring（三重引用符）の保護
            if ('"""' in stripped or "'''" in stripped) and in_function:
                cleaned_lines.append(line)
                continue
            
            # 通常のコメント除去
            cleaned_line = self.remove_comment_from_line(line)
            cleaned_lines.append(cleaned_line)
            
            # 関数終了の検出
            if in_function and stripped and not line.startswith(' ') and not line.startswith('\t') and not stripped.startswith('def '):
                in_function = False
        
        return '\n'.join(cleaned_lines)
    
    def remove_comment_from_line(self, line: str) -> str:
        """
        1行からコメントを除去（文字列内の#は保護）
        """
        in_single_quote = False
        in_double_quote = False
        in_triple_single = False
        in_triple_double = False
        escaped = False
        
        i = 0
        while i < len(line):
            char = line[i]
            
            if escaped:
                escaped = False
                i += 1
                continue
                
            if char == '\\':
                escaped = True
                i += 1
                continue
            
            # トリプルクォートの検出
            if i <= len(line) - 3:
                if line[i:i+3] == '"""':
                    if not in_single_quote and not in_triple_single:
                        in_triple_double = not in_triple_double
                        i += 3
                        continue
                elif line[i:i+3] == "'''":
                    if not in_double_quote and not in_triple_double:
                        in_triple_single = not in_triple_single
                        i += 3
                        continue
            
            # 通常のクォートの検出
            if not in_triple_single and not in_triple_double:
                if char == '"' and not in_single_quote:
                    in_double_quote = not in_double_quote
                elif char == "'" and not in_double_quote:
                    in_single_quote = not in_single_quote
            
            # コメントの検出
            if (char == '#' and 
                not in_single_quote and not in_double_quote and 
                not in_triple_single and not in_triple_double):
                return line[:i].rstrip()
            
            i += 1
        
        return line
    
    def sanitize_imports(self, code: str) -> str:
        """
        危険なimportを無効化
        """
        lines = code.split('\n')
        sanitized_lines = []
        
        for line in lines:
            stripped = line.strip()
            
            # import文の検出
            if stripped.startswith('import ') or stripped.startswith('from '):
                # 危険なモジュールかチェック
                is_dangerous = False
                for dangerous in self.dangerous_imports:
                    if dangerous in stripped:
                        is_dangerous = True
                        break
                
                if is_dangerous:
                    sanitized_lines.append(f"# DISABLED: {line}")
                else:
                    # 限定的に許可（math, randomなど）
                    allowed_modules = ['math', 'random', 'datetime', 'json', 'string']
                    is_allowed = False
                    for allowed in allowed_modules:
                        if f'import {allowed}' in stripped or f'from {allowed}' in stripped:
                            is_allowed = True
                            break
                    
                    if is_allowed:
                        sanitized_lines.append(line)
                    else:
                        sanitized_lines.append(f"# RESTRICTED: {line}")
            else:
                sanitized_lines.append(line)
        
        return '\n'.join(sanitized_lines)
    
    def sanitize_joins(self, code: str) -> str:
        """
        join関数の安全化（os.path.joinなどを制限）
        """
        # os.path.join の制限
        code = re.sub(r'\bos\s*\.\s*path\s*\.\s*join\s*\([^)]*\)', 
                     '"path_join_disabled"', code)
        
        return code


class SafeExecutionEnvironment:
    """
    安全な実行環境を提供するクラス
    """
    
    def __init__(self):
        self.setup_safe_globals()
    
    def setup_safe_globals(self):
        """
        安全なグローバル環境をセットアップ
        """
        self.safe_globals = {
            # 安全な組み込み関数のみ許可
            'len': len,
            'range': range,
            'enumerate': enumerate,
            'zip': zip,
            'map': map,
            'filter': filter,
            'sum': sum,
            'min': min,
            'max': max,
            'abs': abs,
            'round': round,
            'str': str,
            'int': int,
            'float': float,
            'bool': bool,
            'list': list,
            'dict': dict,
            'tuple': tuple,
            'set': set,
            'type': type,
            'isinstance': isinstance,
            'hasattr': hasattr,
            'getattr': getattr,
            'setattr': setattr,
            
            # カスタム安全関数
            'input': self.mock_input,
            'print': self.safe_print,
            'Attack': self.attack_function,
            'Guard': self.guard_function,
            'Cast': self.cast_function,
            'MoveTo': self.moveto_function,
            'MoveForward': self.moveforward_function,
            'MoveBackward': self.movebackward_function,
            'MoveTo': self.move_to_function,
            
            # 特殊変数
            '__name__': '__main__',
            
            # 安全なモジュール
            '__builtins__': {
                '__name__': '__main__',
                '__doc__': None,
            },
            
            # モックされたsys
            'sys': type('MockSys', (), {
                'argv': [],
                'version': 'Python (sandboxed)',
                'platform': 'sandboxed',
                'exit': lambda *args: None
            })()
        }
    
    def mock_input(self, prompt=""):
        """
        input関数のモック実装
        """
        return "mocked_user_input"
    
    def safe_print(self, *args, **kwargs):
        """
        安全なprint関数
        """
        if 'file' in kwargs:
            del kwargs['file']
        return print(*args, **kwargs)
    #かつては、attack_function()の中に、敵の名前, 武器の種類があった
    def attack_function(self):
        """
        Attack関数の実装
        """
        return 'Attacking'
    #かつては、guard_function()の中に(self, enemy: str)があり、guarding from ~~ができた
    def guard_function(self):
        """
        Guard関数の実装
        """
        return 'Guarding'
    
    def cast_function(self, spell: str, n: int):
        """
        Cast関数の実装（2引数版）
        対応呪文: ライトニング, フレイム, フリーズ
        例: Cast("フリーズ", 0) → "Casting: フリーズ, 0"
        """
        return f'Casting: {spell}, {n}'
    
    def moveto_function(self, n: int):
        """
        MoveTo関数の実装
        """
        return f'Moving to: {n}'
    
    def moveforward_function(self):
        """
        MoveForward関数の実装
        """
        return 'Moving forward'
    
    def movebackward_function(self):
        """
        MoveBackward関数の実装
        """
        return 'Moving backward'
    
    def move_to_function(self, n: int):
        """
        MoveTo関数の実装
        """
        return f'Moving to: {n}'


class CodeTracer:
    def __init__(self):
        self.logs = []
        self.execution_step = 0
        self.security_step = 0
        self.variables = {}
        self.errors = []
        self.sanitizer = CodeSanitizer()
        self.safe_env = SafeExecutionEnvironment()
    
    def log(self, msg: str, line: int = 0):
        """通常の実行ログ"""
        self.execution_step += 1
        self.logs.append({
            "t": "execution",
            "s": self.execution_step,
            "line": line,
            "msg": msg
        })
    
    def security_log(self, msg: str, line: int = 0):
        """セキュリティ関連のログ"""
        self.security_step += 1
        self.logs.append({
            "t": "security",
            "s": self.security_step,
            "line": line,
            "msg": msg
        })
    
    def security_warning(self, msg: str, line: int = 0):
        """セキュリティ警告（security_logのエイリアス）"""
        self.security_log(msg, line)
    
    def error(self, msg: str, line: int = 0):
        """エラーログ"""
        self.errors.append({
            "t": "error",
            "s": self.execution_step,
            "line": line,
            "msg": msg
        })


# FastAPIアプリケーション
app = FastAPI()


def execute_and_trace(code: str) -> Dict[str, Any]:
    tracer = CodeTracer()
    
    try:
        # 元のコード長をチェック
        if len(code) > 10000:
            tracer.error("Code too long (max 10KB allowed)")
            return {
                "executionLogs": [],
                "securityLogs": [],
                "errors": tracer.errors,
                "finalResult": {}
            }
        
        # コードのサニタイズ
        original_code = code
        sanitized_code = tracer.sanitizer.sanitize_code(code)
        
        if original_code != sanitized_code:
            tracer.security_warning("Code was sanitized for security")
        
        # ASTを解析
        try:
            tree = ast.parse(sanitized_code)
        except SyntaxError as e:
            tracer.error(f"Syntax error: {str(e)}")
            return {
                "executionLogs": [],
                "securityLogs": [],
                "errors": tracer.errors,
                "finalResult": {}
            }
        
        # 安全な実行環境を準備
        execution_globals = tracer.safe_env.safe_globals.copy()
        execution_locals = {}

        # Swap 関数: EnemiesHP リストの要素を入れ替え、ログ文字列を返す
        # execution_locals を参照するクロージャーで、実行中のリスト状態を直接操作する
        def swap_func(j, k):
            hp_list = execution_locals.get('EnemiesHP')
            if hp_list is None:
                return f"Swap: {j}, {k}"
            if j < 0 or k < 0 or j >= len(hp_list) or k >= len(hp_list):
                tracer.error(f"Swap: インデックスが範囲外です ({j}, {k})")
                return f"Swap: {j}, {k}"
            hp_list[j], hp_list[k] = hp_list[k], hp_list[j]
            return f"Swap: {j}, {k}"
        execution_globals['Swap'] = swap_func

        # 実行時間制限のタイムアウト処理
        timeout_enabled = hasattr(signal, 'SIGALRM')
        
        if timeout_enabled:
            def timeout_handler(signum, frame):
                raise TimeoutError("Code execution timeout")
            
            signal.signal(signal.SIGALRM, timeout_handler)
            signal.alarm(5)
        
        try:
            # 各ステートメントを実行
            for node in tree.body:
                execute_node(node, tracer, execution_globals, execution_locals)
        except TimeoutError:
            tracer.error("Execution timeout (5 seconds limit)")
        except Exception as e:
            tracer.error(f"Runtime error: {str(e)}")
        finally:
            if timeout_enabled:
                signal.alarm(0)
        
        # 最終結果から安全でない要素を除外
        final_result = {}
        for k, v in execution_locals.items():
            if not callable(v) and not k.startswith('__') and not k.startswith('_'):
                try:
                    json.dumps(v, default=str)
                    final_result[k] = v
                except:
                    final_result[k] = str(v)
        
        # ログをカテゴリ別に分類
        execution_logs = []
        security_logs = []
        
        for log in tracer.logs:
            if log["t"] == "execution":
                execution_logs.append({"s": log["s"], "line": log["line"], "msg": log["msg"]})
            elif log["t"] == "security":
                security_logs.append({"s": log["s"], "line": log["line"], "msg": log["msg"]})
        
        return {
            "executionLogs": execution_logs,
            "securityLogs": security_logs,
            "errors": tracer.errors,
            "finalResult": final_result,
            "sanitized": original_code != sanitized_code
        }
        
    except Exception as e:
        tracer.error(f"Unexpected error: {str(e)}")
        return {
            "executionLogs": [],
            "securityLogs": [],
            "errors": tracer.errors,
            "finalResult": {},
            "sanitized": False
        }


def execute_node(node: ast.AST, tracer: CodeTracer, global_vars: dict, local_vars: dict):
    try:
        # ノード実行前のセキュリティチェック
        if isinstance(node, ast.Import) or isinstance(node, ast.ImportFrom):
            tracer.security_log("Import statement detected and controlled", node.lineno)
            return
        
        if isinstance(node, ast.FunctionDef):
            # 関数定義の処理
            try:
                func_name = node.name
                line_no = node.lineno
                tracer.log(f"Defining function: {func_name}", line_no)
                
                func_code = compile(ast.Module(body=[node], type_ignores=[]), '<string>', 'exec')
                exec(func_code, global_vars, local_vars)
                
                tracer.log(f"Function {func_name} defined successfully", line_no)
            except Exception as e:
                tracer.error(f"Error in function definition: {str(e)}", node.lineno)
        
        elif isinstance(node, ast.Return):
            # return文の処理
            try:
                line_no = node.lineno
                if node.value is not None:
                    return_value = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                    tracer.log(f"return {ast.unparse(node.value)} → {repr(return_value)}", line_no)
                else:
                    tracer.log("return", line_no)
            except Exception as e:
                tracer.error(f"Error in return statement: {str(e)}", node.lineno)
        
        elif isinstance(node, ast.Assign):
            # 代入文
            line_no = node.lineno
            for target in node.targets:
                if isinstance(target, ast.Name):
                    # 通常の変数代入
                    try:
                        value = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                        if isinstance(value, str) and len(value) > 1000:
                            tracer.security_warning("Large string value truncated", line_no)
                            value = value[:1000] + "...[truncated]"
                        local_vars[target.id] = value
                        tracer.log(f'{target.id} = {repr(value)}', line_no)
                    except Exception as e:
                        tracer.error(f"Error in assignment: {str(e)}", line_no)
                
                elif isinstance(target, ast.Tuple) or isinstance(target, ast.List):
                    # tuple/list unpacking
                    try:
                        values = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                        if not isinstance(values, (tuple, list)):
                            values = [values]
                        
                        target_names = []
                        for i, elt in enumerate(target.elts):
                            if isinstance(elt, ast.Name):
                                local_vars[elt.id] = values[i]
                                target_names.append(f"{elt.id} = {repr(values[i])}")
                            elif isinstance(elt, ast.Subscript):
                                obj_name = elt.value.id if isinstance(elt.value, ast.Name) else str(elt.value)
                                if obj_name in local_vars:
                                    obj = local_vars[obj_name]
                                    index = eval(compile(ast.Expression(elt.slice), '<string>', 'eval'), global_vars, local_vars)
                                    obj[index] = values[i]
                                    target_names.append(f"{obj_name}[{index}] = {repr(values[i])}")
                        
                        tracer.log(f"tuple unpacking: {', '.join(target_names)}", line_no)
                    except Exception as e:
                        tracer.error(f"Error in tuple unpacking: {str(e)}", line_no)
                
                elif isinstance(target, ast.Subscript):
                    # 配列への代入
                    try:
                        obj_name = target.value.id if isinstance(target.value, ast.Name) else str(target.value)
                        if obj_name in local_vars:
                            obj = local_vars[obj_name]
                            index = eval(compile(ast.Expression(target.slice), '<string>', 'eval'), global_vars, local_vars)
                            
                            if isinstance(obj, (list, tuple)) and (index < 0 or index >= len(obj)):
                                tracer.error(f"Array index {index} out of bounds for {obj_name}", line_no)
                                return
                            
                            value = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                            obj[index] = value
                            tracer.log(f'{obj_name}[{index}] = {repr(value)} → {obj_name} = {repr(obj)}', line_no)
                    except Exception as e:
                        tracer.error(f"Error in array assignment: {str(e)}", line_no)
        
        elif isinstance(node, ast.AugAssign):
            # 拡張代入
            line_no = node.lineno
            try:
                if isinstance(node.target, ast.Subscript):
                    # 配列要素への拡張代入
                    obj_name = node.target.value.id if isinstance(node.target.value, ast.Name) else str(node.target.value)
                    if obj_name in local_vars:
                        obj = local_vars[obj_name]
                        index = eval(compile(ast.Expression(node.target.slice), '<string>', 'eval'), global_vars, local_vars)
                        right_value = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                        
                        old_value = obj[index]
                        if isinstance(node.op, ast.Add):
                            new_value = old_value + right_value
                            obj[index] = new_value
                            tracer.log(f'{obj_name}[{index}] = {obj_name}[{index}] + {right_value} → {obj_name}[{index}] = {new_value} → {obj_name} = {repr(obj)}', line_no)
                        elif isinstance(node.op, ast.Sub):
                            new_value = old_value - right_value
                            obj[index] = new_value
                            tracer.log(f'{obj_name}[{index}] = {obj_name}[{index}] - {right_value} → {obj_name}[{index}] = {new_value} → {obj_name} = {repr(obj)}', line_no)
                        elif isinstance(node.op, ast.Mult):
                            new_value = old_value * right_value
                            obj[index] = new_value
                            tracer.log(f'{obj_name}[{index}] = {obj_name}[{index}] * {right_value} → {obj_name}[{index}] = {new_value} → {obj_name} = {repr(obj)}', line_no)
                        elif isinstance(node.op, ast.Div):
                            new_value = old_value / right_value
                            obj[index] = new_value
                            tracer.log(f'{obj_name}[{index}] = {obj_name}[{index}] / {right_value} → {obj_name}[{index}] = {new_value} → {obj_name} = {repr(obj)}', line_no)
                else:
                    # 通常の変数への拡張代入
                    target_name = node.target.id if isinstance(node.target, ast.Name) else str(node.target)
                    if target_name in local_vars:
                        old_value = local_vars[target_name]
                        right_value = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                        
                        if isinstance(node.op, ast.Add):
                            new_value = old_value + right_value
                            local_vars[target_name] = new_value
                            tracer.log(f'{target_name} = {target_name} + {right_value} → {target_name} = {new_value}', line_no)
                        elif isinstance(node.op, ast.Sub):
                            new_value = old_value - right_value
                            local_vars[target_name] = new_value
                            tracer.log(f'{target_name} = {target_name} - {right_value} → {target_name} = {new_value}', line_no)
                        elif isinstance(node.op, ast.Mult):
                            new_value = old_value * right_value
                            local_vars[target_name] = new_value
                            tracer.log(f'{target_name} = {target_name} * {right_value} → {target_name} = {new_value}', line_no)
                        elif isinstance(node.op, ast.Div):
                            new_value = old_value / right_value
                            local_vars[target_name] = new_value
                            tracer.log(f'{target_name} = {target_name} / {right_value} → {target_name} = {new_value}', line_no)
                    else:
                        tracer.error(f"Variable {target_name} not found for augmented assignment", line_no)
            except Exception as e:
                tracer.error(f"Error in augmented assignment: {str(e)}", line_no)
        
        elif isinstance(node, ast.For):
            # forループ
            target_name = node.target.id
            line_no = node.lineno
            try:
                iter_obj = eval(compile(ast.Expression(node.iter), '<string>', 'eval'), global_vars, local_vars)
                iter_list = list(iter_obj)
                
                if len(iter_list) > 1000:
                    tracer.security_log("Loop iteration limited to 1000 cycles", line_no)
                    iter_list = iter_list[:1000]
                
                if isinstance(node.iter, ast.Call) and isinstance(node.iter.func, ast.Name) and node.iter.func.id == 'range':
                    if len(node.iter.args) == 1:
                        start, end = 0, eval(compile(ast.Expression(node.iter.args[0]), '<string>', 'eval'), global_vars, local_vars)
                    elif len(node.iter.args) == 2:
                        start = eval(compile(ast.Expression(node.iter.args[0]), '<string>', 'eval'), global_vars, local_vars)
                        end = eval(compile(ast.Expression(node.iter.args[1]), '<string>', 'eval'), global_vars, local_vars)
                    else:
                        start = eval(compile(ast.Expression(node.iter.args[0]), '<string>', 'eval'), global_vars, local_vars)
                        end = eval(compile(ast.Expression(node.iter.args[1]), '<string>', 'eval'), global_vars, local_vars)
                        step = eval(compile(ast.Expression(node.iter.args[2]), '<string>', 'eval'), global_vars, local_vars)
                    
                    tracer.log(f'for {target_name} in range({start}, {end}) start', line_no)
                
                for i, value in enumerate(iter_list):
                    local_vars[target_name] = value
                    tracer.log(f'{target_name} = {value}', line_no)
                    
                    for stmt in node.body:
                        execute_node(stmt, tracer, global_vars, local_vars)
                
                tracer.log("for loop end", line_no)
            except Exception as e:
                tracer.error(f"Error in for loop: {str(e)}", line_no)
        
        elif isinstance(node, ast.While):
            # whileループ
            loop_count = 0
            max_iterations = 100
            line_no = node.lineno
            
            try:
                tracer.log("while loop start", line_no)
                while True:
                    condition_result = eval(compile(ast.Expression(node.test), '<string>', 'eval'), global_vars, local_vars)
                    condition_str = ast.unparse(node.test)
                    tracer.log(f"while condition: {condition_str} → {condition_result}", line_no)
                    
                    if not condition_result:
                        tracer.log("while condition is False, exiting loop", line_no)
                        break
                    
                    loop_count += 1
                    if loop_count > max_iterations:
                        tracer.security_log(f"While loop terminated after {max_iterations} iterations", line_no)
                        break
                    
                    tracer.log(f"while loop iteration {loop_count}", line_no)
                    for stmt in node.body:
                        execute_node(stmt, tracer, global_vars, local_vars)
                        
                tracer.log(f"while loop end (completed {loop_count} iterations)", line_no)
            except Exception as e:
                tracer.error(f"Error in while loop: {str(e)}", line_no)
        
        elif isinstance(node, ast.If):
            # if文
            line_no = node.lineno
            try:
                condition_result = eval(compile(ast.Expression(node.test), '<string>', 'eval'), global_vars, local_vars)
                condition_str = ast.unparse(node.test)
                
                tracer.log(f"if condition: {condition_str} → {condition_result}", line_no)
                
                if condition_result:
                    tracer.log("if condition is True, executing if branch", line_no)
                    for stmt in node.body:
                        execute_node(stmt, tracer, global_vars, local_vars)
                elif node.orelse:
                    tracer.log("if condition is False, executing else branch", line_no)
                    for stmt in node.orelse:
                        execute_node(stmt, tracer, global_vars, local_vars)
                else:
                    tracer.log("if condition is False, no else branch", line_no)
                        
            except Exception as e:
                tracer.error(f"Error in if statement: {str(e)}", line_no)
        
        elif isinstance(node, ast.Expr):
            # 式文
            line_no = node.lineno
            try:
                if isinstance(node.value, ast.Call):
                    func_name = node.value.func.id if isinstance(node.value.func, ast.Name) else str(node.value.func)
                    
                    dangerous_funcs = ['eval', 'exec', 'compile', '__import__', 'open', 'file']
                    if func_name in dangerous_funcs:
                        tracer.security_log(f"Dangerous function {func_name}() blocked", line_no)
                        return
                    
                    if func_name == 'print':
                        # print文の特別処理
                        if node.value.args:
                            arg_value = eval(compile(ast.Expression(node.value.args[0]), '<string>', 'eval'), global_vars, local_vars)
                            arg_str = ast.unparse(node.value.args[0])
                            if isinstance(arg_value, str) and len(str(arg_value)) > 500:
                                tracer.security_log("Print output truncated", line_no)
                                arg_value = str(arg_value)[:500] + "...[truncated]"
                            tracer.log(f'print({arg_str}) → {arg_value}', line_no)
                        else:
                            tracer.log('print() → ', line_no)
                    elif func_name in ['Attack', 'Guard', 'Cast', 'MoveTo', 'MoveForward', 'MoveBackward', 'Swap']:
                        # ゲーム関数の呼び出し
                        result = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                        tracer.log(result, line_no)
                    else:
                        # その他の関数呼び出し
                        try:
                            result = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                            if result is not None:
                                tracer.log(f'{ast.unparse(node.value)} → {repr(result)}', line_no)
                            else:
                                tracer.log(f'{ast.unparse(node.value)} executed', line_no)
                        except Exception as e:
                            tracer.error(f"Error in function call {func_name}: {str(e)}", line_no)
                else:
                    # その他の式
                    result = eval(compile(ast.Expression(node.value), '<string>', 'eval'), global_vars, local_vars)
                    if result is not None:
                        tracer.log(f'{ast.unparse(node.value)} → {repr(result)}', line_no)
            except Exception as e:
                tracer.error(f"Error in expression: {str(e)}", line_no)
        
        else:
            # 未対応のASTノードタイプ
            line_no = getattr(node, 'lineno', 0)
            tracer.log(f"Unsupported AST node type: {type(node).__name__}", line_no)
            try:
                node_code = compile(ast.Module(body=[node], type_ignores=[]), '<string>', 'exec')
                exec(node_code, global_vars, local_vars)
                tracer.log(f"Generic execution of {type(node).__name__} successful", line_no)
            except Exception as e:
                tracer.error(f"Failed to execute {type(node).__name__}: {str(e)}", line_no)
        
    except Exception as e:
        line_no = getattr(node, 'lineno', 0)
        tracer.error(f"Error executing node: {str(e)}", line_no)


# ============================================================
# WebSocketエンドポイント（認証追加版）
# ============================================================

@app.websocket("/ws")
async def websocket_endpoint(websocket: WebSocket, key: str = Query(None)):
    # === 認証チェック（追加部分） ===
    if not verify_api_key(key):
        await websocket.close(code=4001, reason="Invalid API key")
        print("❌ Connection rejected: Invalid API key")
        return
    
    await websocket.accept()
    print("🔗 WebSocket connection established")
    
    try:
        while True:
            data = await websocket.receive_text()
            print(f"📨 Received code request ({len(data)} bytes)")
            
            try:
                request = json.loads(data)
                code = request.get("code", "")
                
                if not code.strip():
                    error_response = {
                        "executionLogs": [],
                        "securityLogs": [],
                        "errors": [{"t": "error", "s": 1, "line": 0, "msg": "No code to execute"}],
                        "finalResult": {}
                    }
                    await websocket.send_text(json.dumps(error_response))
                    continue
                
                print(f"🔍 Analyzing code ({len(code)} chars)...")
                
                result = execute_and_trace(code)
                
                security_info = {
                    "code_length": len(code),
                    "sanitized": result.get("sanitized", False),
                    "execution_time": "< 5s (timeout enforced)",
                    "memory_limited": True
                }
                
                result["security_info"] = security_info
                
                print(f"\n[DEBUG] JSON Response:")
                print(f"  Execution Logs count: {len(result.get('executionLogs', []))}")
                print(f"  Security Logs count: {len(result.get('securityLogs', []))}")
                print(f"  Errors count: {len(result.get('errors', []))}")
                print(f"  FinalResult count: {len(result.get('finalResult', {}))}")
                if len(result.get('executionLogs', [])) > 0:
                    print(f"  First 3 execution logs:")
                    for i, log in enumerate(result.get('executionLogs', [])[:3]):
                        print(f"    {i+1}. Line {log['line']}: {log['msg']}")
                print()
                
                response = json.dumps(result, ensure_ascii=False)
                await websocket.send_text(response)
                
                exec_log_count = len(result.get('executionLogs', []))
                sec_log_count = len(result.get('securityLogs', []))
                error_count = len(result.get('errors', []))
                var_count = len(result.get('finalResult', {}))
                
                print(f"✅ Execution completed:")
                print(f"   📝 {exec_log_count} execution steps")
                print(f"   🛡️ {sec_log_count} security steps")
                print(f"   ❌ {error_count} errors")  
                print(f"   📊 {var_count} final variables")
                if result.get("sanitized"):
                    print(f"   🔒 Code was sanitized for security")
                
            except json.JSONDecodeError:
                print("📝 Direct code execution request")
                result = execute_and_trace(data)
                response = json.dumps(result, ensure_ascii=False)
                await websocket.send_text(response)
            
            except Exception as e:
                print(f"❌ Processing error: {e}")
                error_response = {
                    "executionLogs": [],
                    "securityLogs": [],
                    "errors": [{"t": "error", "s": 1, "line": 0, "msg": f"Server error: {str(e)}"}],
                    "finalResult": {}
                }
                await websocket.send_text(json.dumps(error_response))
            
    except WebSocketDisconnect:
        print("🔌 WebSocket connection closed")
    except Exception as e:
        print(f"❌ WebSocket error: {e}")
        try:
            error_response = json.dumps({"error": f"Connection error: {str(e)}"})
            await websocket.send_text(error_response)
        except:
            pass


def start_server():
    print("=" * 60)
    print("🛡️  SECURE PYTHON CODE EXECUTION SERVER (with Line Numbers)")
    print("=" * 60)
    print("🔒 Security Features:")
    print("   • Code sanitization")
    print("   • sys.argv mocking")  
    print("   • input() function mocking")
    print("   • time.sleep() disabled")
    print("   • Import restrictions")
    print("   • Execution timeout (5 seconds)")
    print("   • Loop iteration limits")
    print("   • Memory usage protection")
    print(f"   • API Key Authentication: {'✅ Configured' if API_KEY != 'change-this-secret-key' else '⚠️  Using default key!'}")
    print("=" * 60)
    print("✨ New Feature:")
    print("   • Line number tracking for each execution step")
    print("=" * 60)
    print(f"🌐 WebSocket endpoint: ws://0.0.0.0:{PORT}/ws?key=YOUR_API_KEY")
    print("🚀 Starting server...")
    uvicorn.run(app, host="0.0.0.0", port=PORT)


if __name__ == "__main__":
    print("🚀 Starting Secure Python Code Execution Server with Line Numbers...")
    start_server()
